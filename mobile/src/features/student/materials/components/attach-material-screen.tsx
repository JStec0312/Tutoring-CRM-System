import { useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useState } from 'react';
import { Text, View } from 'react-native';
import type { ApiClient } from '@/shared/api/api-client';
import { useApi } from '@/shared/api/api-context';
import { WorkspaceButton, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { useWorkspaceQuery } from '@/shared/models/use-workspace-query';
import { useWorkspaceMutation } from '@/shared/models/use-workspace-mutation';
import { attachMaterial, getMaterials } from '@/features/tutor/materials/api/materials';
import { AgreementLessonPicker } from '@/features/tutor/materials/components/agreement-lesson-picker';
import { StudentPage, StudentQueryFeedback } from '../../workspace/components/student-workspace';
import { getStudentLessons } from '../../lesson/api/lessons';

export function StudentAttachMaterialScreen() {
  const { agreementId, materialId, lessonId: initialLesson } = useLocalSearchParams<{ agreementId?: string; materialId: string; lessonId?: string }>();
  const router = useRouter();
  const api = useApi();
  const [lessonId, setLessonId] = useState<string | undefined>(initialLesson);
  const mutation = useWorkspaceMutation();
  const load = useCallback(async (client: ApiClient, signal: AbortSignal) => {
    if (!agreementId) return undefined;
    const [materials, lessons] = await Promise.all([getMaterials(client, agreementId, signal), getStudentLessons(client, agreementId, signal)]);
    const material = materials.find(item => item.materialId === materialId);
    return { material, lessons };
  }, [agreementId, materialId]);
  const query = useWorkspaceQuery(load);
  function back() {
    if (initialLesson) {
      router.replace({ pathname: '/student/lessons/[lessonId]', params: { lessonId: initialLesson } });
    } else {
      router.replace({ pathname: '/student/lessons', params: { agreementId } });
    }
  }
  async function attach() {
    if (!query.data?.material || !lessonId || !query.data.lessons.some(lesson => lesson.lessonId === lessonId)) return;
    await attachMaterial(api, materialId, lessonId);
    back();
  }
  return <StudentPage title="Attach material to lesson">
    <StudentQueryFeedback {...query} />
    {!agreementId && <Text style={ui.body}>Open a lesson to choose a material first.</Text>}
    {query.data && !query.data.material && <Text role="alert" style={ui.error}>Material not found in this agreement.</Text>}
    {query.data?.material && <>
      <Text style={ui.body}>{query.data.material.title}</Text>
      <AgreementLessonPicker lessons={query.data.lessons} selected={lessonId} onSelect={setLessonId} disabled={mutation.busy} />
    </>}
    {!!mutation.error && <Text role="alert" style={ui.error}>{mutation.error}</Text>}
    <View style={ui.row}>
      <WorkspaceButton primary label="Attach material" disabled={mutation.busy || !query.data?.material || !lessonId}
        onPress={() => void mutation.run(attach)} />
      <WorkspaceButton label="Cancel" disabled={mutation.busy} onPress={back} />
    </View>
  </StudentPage>;
}
