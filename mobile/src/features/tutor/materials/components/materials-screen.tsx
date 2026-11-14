import { useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useRef, useState } from 'react';
import { Text, useWindowDimensions, View } from 'react-native';
import { useApi } from '@/shared/api/api-context';
import type { ApiClient } from '@/shared/api/api-client';
import { WorkspaceButton, WorkspaceCard, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { downloadMaterial, getMaterials, type Material } from '../api/materials';
import { saveMaterialDownload } from '../models/save-material-download';
import { getStudents } from '../../students/api/get-students';
import { WorkspaceTable } from '../../workspace/components/workspace-data';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';
import { errorMessage, useTutorQuery } from '../../workspace/models/use-tutor-query';

export function MaterialsScreen() {
  const { agreementId: initialAgreement, lessonId } = useLocalSearchParams<{ agreementId?: string; lessonId?: string }>();
  const router = useRouter();
  const compact = useWindowDimensions().width < 760;
  const students = useTutorQuery(getStudents);
  const agreements = students.data?.flatMap(student => student.agreements.map(agreement => ({
    id: agreement.tutoringAgreementId, label: `${student.displayName} · ${agreement.subject}`,
  }))) ?? [];
  const [selected, setSelected] = useState<string>();
  const agreementId = selected ?? initialAgreement ?? agreements[0]?.id;
  const targetLesson = agreementId === initialAgreement ? lessonId : undefined;
  const selectedAgreement = agreements.find(agreement => agreement.id === agreementId);
  const uploadButton = agreementId ? <WorkspaceButton label="Upload material" primary onPress={() =>
    router.push({ pathname: '/tutor/materials/upload', params: { agreementId, lessonId: targetLesson } })} /> : null;

  return <TutorPage title={compact ? 'Materials' : 'Learning materials'} subtitle={compact ? selectedAgreement?.label : undefined}>
    <QueryFeedback {...students} />
    {students.data && !agreements.length && <Text style={ui.body}>Add a tutoring agreement to share materials.</Text>}
    {compact ? uploadButton : <View style={ui.row}>{uploadButton}</View>}
    {!!agreements.length && (!compact || agreements.length > 1) && <View style={ui.stack}>
      <Text style={ui.sectionHeading}>Tutoring agreement</Text>
      <View style={ui.row}>
        {agreements.map(({ id, label }) => <WorkspaceButton key={id} label={label}
          primary={agreementId === id} onPress={() => setSelected(id)} />)}
      </View>
    </View>}
    {agreementId && <MaterialList key={agreementId} agreementId={agreementId} lessonId={targetLesson} />}
  </TutorPage>;
}

export function MaterialList({ agreementId, lessonId, attachedOnly = false }: {
  agreementId: string; lessonId?: string; attachedOnly?: boolean;
}) {
  const api = useApi();
  const router = useRouter();
  const compact = useWindowDimensions().width < 760;
  const load = useCallback((client: ApiClient, signal: AbortSignal) => getMaterials(client, agreementId, signal), [agreementId]);
  const query = useTutorQuery(load);
  const [error, setError] = useState('');
  const [downloading, setDownloading] = useState<string>();
  const inFlight = useRef(false);
  const materials = query.data?.filter(material => !attachedOnly || material.lessonId === lessonId) ?? [];

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
      {!attachedOnly && <>
        <WorkspaceButton label="Attach" onPress={() => router.push({ pathname: '/tutor/materials/[materialId]/attach',
          params: { agreementId, materialId: material.materialId, lessonId } })} />
        <WorkspaceButton label="Delete" onPress={() => router.push({ pathname: '/tutor/materials/[materialId]/delete',
          params: { agreementId, materialId: material.materialId } })} />
      </>}
    </View>;
  }

  const summary = (material: Material) => [
    `${material.fileName} · ${(material.sizeBytes / 1000).toFixed(0)} KB`, material.uploadedByTutor ? 'Tutor' : 'Student',
    material.lessonId ? 'Attached' : 'Not attached',
  ];

  return <View style={ui.stack}>
    <QueryFeedback {...query} />
    {query.data && !materials.length && <Text style={ui.body}>{attachedOnly ? 'No materials attached to this lesson.' : 'No materials in this agreement.'}</Text>}
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
