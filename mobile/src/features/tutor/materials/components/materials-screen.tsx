import { useRouter } from 'expo-router';
import { useCallback, useRef, useState } from 'react';
import { Text, useWindowDimensions, View } from 'react-native';
import { useApi } from '@/shared/api/api-context';
import type { ApiClient } from '@/shared/api/api-client';
import { WorkspaceButton, WorkspaceCard, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { downloadMaterial, getMaterials, type Material } from '../api/materials';
import { saveMaterialDownload } from '../models/save-material-download';
import { WorkspaceTable } from '../../workspace/components/workspace-data';
import { QueryFeedback } from '../../workspace/components/tutor-workspace';
import { errorMessage, useTutorQuery } from '../../workspace/models/use-tutor-query';

export function MaterialList({ agreementId, lessonId, lessonContext = false }: {
  agreementId: string; lessonId?: string; lessonContext?: boolean;
}) {
  const api = useApi();
  const router = useRouter();
  const compact = useWindowDimensions().width < 760;
  const load = useCallback((client: ApiClient, signal: AbortSignal) => getMaterials(client, agreementId, signal), [agreementId]);
  const query = useTutorQuery(load);
  const [error, setError] = useState('');
  const [downloading, setDownloading] = useState<string>();
  const inFlight = useRef(false);
  const materials = query.data?.filter(material => !lessonContext || material.lessonId === lessonId || !material.lessonId) ?? [];

  async function download(material: Material) {
    if (inFlight.current) return;
    inFlight.current = true;
    setDownloading(material.materialId);
    setError('');
    try {
      const bytes = await downloadMaterial(api, material.materialId);
      await saveMaterialDownload(material, bytes);
    } catch (error) {
      setError(errorMessage(error));
    } finally {
      setDownloading(undefined);
      inFlight.current = false;
    }
  }

  function actions(material: Material) {
    return <View key="actions" style={compact ? ui.stack : ui.row}>
      <WorkspaceButton label={downloading === material.materialId ? 'Downloading…' : 'Download file'} disabled={!!downloading}
        onPress={() => void download(material)} />
      {(!lessonContext || !material.lessonId) && <WorkspaceButton label="Attach" onPress={() => router.push({ pathname: '/tutor/materials/[materialId]/attach',
          params: { agreementId, materialId: material.materialId, lessonId } })} />}
        <WorkspaceButton label="Delete" onPress={() => router.push({ pathname: '/tutor/materials/[materialId]/delete',
          params: { agreementId, materialId: material.materialId, lessonId } })} />
    </View>;
  }

  const summary = (material: Material) => [
    `${material.fileName} · ${(material.sizeBytes / 1000).toFixed(0)} KB`, material.uploadedByTutor ? 'Tutor' : 'Student',
    material.lessonId ? 'Attached' : 'Not attached',
  ];

  return <View style={ui.stack}>
    <QueryFeedback {...query} />
    {query.data && !materials.length && <Text style={ui.body}>No shared materials for this lesson.</Text>}
    {!!error && <Text accessibilityRole="alert" style={ui.error}>{error}</Text>}
    {compact ? materials.map(material => <View key={material.materialId} style={ui.stack}>
      <WorkspaceCard title={material.title} detail={summary(material).join(' · ')} />
      {actions(material)}
    </View>) : <WorkspaceTable headers={['Material', 'File', 'Uploaded by', 'Attachment', 'Actions']} rows={materials.map(material => ({
      key: material.materialId,
      cells: [material.title, ...summary(material), actions(material)],
    }))} />}
  </View>;
}
