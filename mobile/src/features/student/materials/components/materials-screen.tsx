import { useCallback } from 'react';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { Text, View } from 'react-native';
import type { ApiClient } from '@/shared/api/api-client';
import { WorkspaceButton, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { useWorkspaceQuery } from '@/shared/models/use-workspace-query';
import { getMaterials } from '@/features/tutor/materials/api/materials';
import { shortDate } from '@/features/tutor/workspace/models/workspace-format';
import { StudentPage, StudentQueryFeedback } from '../../workspace/components/student-workspace';
import { StudentTable } from '../../workspace/components/student-table';
import { AgreementPicker } from '../../workspace/components/agreement-picker';
import { useStudentAgreements } from '../../workspace/models/use-student-agreements';
import { getStudentLessons } from '../../lesson/api/lessons';
import { MaterialDownloadButton } from './material-download-button';

export function StudentMaterialsScreen() {
  const { agreementId: initialId } = useLocalSearchParams<{ agreementId?: string }>();
  const router = useRouter();
  const agreements = useStudentAgreements(initialId);
  const agreementId = agreements.agreement?.agreementId;
  const load = useCallback(async (api: ApiClient, signal: AbortSignal) => {
    if (!agreementId) return undefined;
    const [materials, lessons] = await Promise.all([getMaterials(api, agreementId, signal), getStudentLessons(api, agreementId, signal)]);
    return { materials, lessons };
  }, [agreementId]);
  const query = useWorkspaceQuery(load);
  const lessons = query.data?.lessons ?? [];
  return <StudentPage code="S05" title="My learning materials">
    <StudentQueryFeedback {...agreements} />
    {agreements.data && <AgreementPicker agreements={agreements.data} selected={agreementId} onSelect={agreements.selectAgreement} />}
    {agreementId && <>
      <View style={ui.row}><WorkspaceButton primary label="Upload material"
        onPress={() => router.push({ pathname: '/student/materials/upload', params: { agreementId } })} /></View>
      <StudentQueryFeedback {...query} />
      {query.data && <StudentTable label="Learning materials" headings={['Material', 'Lesson', 'Action']}
        rows={query.data.materials.map(material => {
          const lesson = lessons.find(item => item.lessonId === material.lessonId);
          let lessonLabel = 'Not attached';
          if (lesson) lessonLabel = shortDate(lesson.startsAtUtc);
          return { key: material.materialId, cells: [material.title, lessonLabel,
            <View key={material.materialId} style={ui.stack}>
              <MaterialDownloadButton material={material} />
              {!material.lessonId && <WorkspaceButton textOnly label="Attach to lesson →" onPress={() => router.push({
                pathname: '/student/materials/[materialId]/attach', params: { materialId: material.materialId, agreementId },
              })} />}
            </View>] };
        })} />}
      {query.data?.materials.length === 0 && <Text style={ui.body}>No shared materials in this agreement.</Text>}
    </>}
  </StudentPage>;
}
