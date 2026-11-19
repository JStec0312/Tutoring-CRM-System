import { useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useState } from 'react';
import { Text, View } from 'react-native';
import { useApi } from '@/shared/api/api-context';
import type { ApiClient } from '@/shared/api/api-client';
import { WorkspaceButton, WorkspaceCard, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';
import { useTutorMutation } from '../../workspace/models/use-tutor-mutation';
import { useTutorQuery } from '../../workspace/models/use-tutor-query';
import { getAgreementLessons } from '../api/get-agreement-lessons';
import { attachMaterial, deleteMaterial, getMaterials } from '../api/materials';
import { AgreementLessonPicker } from './agreement-lesson-picker';

export function AttachMaterialScreen() {
  return <ManageMaterialScreen attach />;
}

export function DeleteMaterialScreen() {
  return <ManageMaterialScreen attach={false} />;
}

function ManageMaterialScreen({ attach }: { attach: boolean }) {
  const { materialId, agreementId, lessonId: initialLesson } =
    useLocalSearchParams<{ materialId: string; agreementId?: string; lessonId?: string }>();
  const router = useRouter();
  const api = useApi();
  const mutation = useTutorMutation();
  const [lessonId, setLessonId] = useState(initialLesson);
  const [deleted, setDeleted] = useState(false);
  const load = useCallback(async (client: ApiClient, signal: AbortSignal) => {
    if (!agreementId) return undefined;
    const [materials, lessons] = await Promise.all([
      getMaterials(client, agreementId, signal), attach ? getAgreementLessons(client, agreementId, signal) : [],
    ]);
    return { material: materials.find(material => material.materialId === materialId), lessons };
  }, [agreementId, attach, materialId]);
  const query = useTutorQuery(load);
  const material = query.data?.material;
  const canAttach = query.data?.lessons.some(lesson => lesson.lessonId === lessonId);
  function back() {
    if (initialLesson) {
      router.replace({ pathname: '/tutor/lessons/[lessonId]', params: { lessonId: initialLesson } });
    } else {
      router.replace({ pathname: '/tutor/lessons', params: { agreementId } });
    }
  }

  async function confirm() {
    if (!material) return;
    if (attach) {
      if (!lessonId || !canAttach) return;
      await attachMaterial(api, materialId, lessonId);
      back();
    } else {
      await deleteMaterial(api, materialId);
      setDeleted(true);
    }
  }

  return <TutorPage title={deleted ? 'Material deleted' : attach ? 'Attach material to lesson' : 'Delete learning material'}>
    <QueryFeedback {...query} />
    {!query.loading && !query.error && !material && <Text style={ui.body}>This material is unavailable.</Text>}
    {material && <WorkspaceCard title={material.title} detail={material.fileName} />}
    {material && attach && <AgreementLessonPicker lessons={query.data?.lessons ?? []} selected={lessonId}
      onSelect={setLessonId} disabled={mutation.busy} />}
    {material && !attach && <Text style={ui.body}>{deleted
      ? 'This file has been removed from the agreement.'
      : 'This file will be removed from the agreement and any lesson it is attached to.'}</Text>}
    {!!mutation.error && <Text accessibilityRole="alert" style={ui.error}>{mutation.error}</Text>}
    <View style={ui.row}>
      {material && !deleted && <WorkspaceButton primary label={attach ? 'Attach material' : 'Confirm deletion'}
        disabled={mutation.busy || (attach && !canAttach)} onPress={() => void mutation.run(confirm)} />}
      <WorkspaceButton label={deleted ? 'Back to lesson' : attach ? 'Cancel' : 'Keep material'}
        disabled={mutation.busy} onPress={back} />
    </View>
  </TutorPage>;
}
