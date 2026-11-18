import { getDocumentAsync, type DocumentPickerAsset } from 'expo-document-picker';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useState } from 'react';
import { Text, View } from 'react-native';
import type { ApiClient } from '@/shared/api/api-client';
import { useApi } from '@/shared/api/api-context';
import { WorkspaceButton, WorkspaceField, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { useWorkspaceQuery } from '@/shared/models/use-workspace-query';
import { useWorkspaceMutation } from '@/shared/models/use-workspace-mutation';
import { uploadMaterial } from '@/features/tutor/materials/api/materials';
import { AgreementLessonPicker } from '@/features/tutor/materials/components/agreement-lesson-picker';
import { StudentPage, StudentQueryFeedback } from '../../workspace/components/student-workspace';
import { getStudentLessons } from '../../lesson/api/lessons';
import { validateMaterial } from '../models/validate-material';

export function StudentUploadMaterialScreen() {
  const { agreementId, lessonId: initialLesson } = useLocalSearchParams<{ agreementId?: string; lessonId?: string }>();
  const router = useRouter();
  const api = useApi();
  const [lessonId, setLessonId] = useState(initialLesson);
  const [title, setTitle] = useState('');
  const [asset, setAsset] = useState<DocumentPickerAsset>();
  const mutation = useWorkspaceMutation();
  const load = useCallback(async (client: ApiClient, signal: AbortSignal) => {
    if (!agreementId) return undefined;
    return getStudentLessons(client, agreementId, signal);
  }, [agreementId]);
  const query = useWorkspaceQuery(load);
  function back() {
    if (initialLesson) router.replace({ pathname: '/student/lessons/[lessonId]', params: { lessonId: initialLesson } });
    else router.replace({ pathname: '/student/materials', params: { agreementId } });
  }
  async function chooseFile() {
    const result = await getDocumentAsync({ type: ['application/pdf', 'image/jpeg', 'image/png'], base64: false, copyToCacheDirectory: true });
    if (result.canceled) return;
    const picked = result.assets[0];
    const error = validateMaterial(picked);
    setAsset(undefined);
    if (error) { mutation.setError(error); return; }
    setAsset(picked);
  }
  async function submit() {
    if (!agreementId || !query.data || !asset || !title.trim()) return;
    if (lessonId && !query.data.some(lesson => lesson.lessonId === lessonId)) {
      mutation.setError('Choose a lesson in this agreement or clear the selection.');
      return;
    }
    await uploadMaterial(api, agreementId, title.trim(), asset, lessonId);
    back();
  }
  let fileLabel = 'Choose file';
  if (asset) fileLabel = 'Choose another file';
  return <StudentPage title="Upload learning material">
    <StudentQueryFeedback {...query} />
    {!agreementId && <Text style={ui.body}>Choose an agreement from Materials first.</Text>}
    {query.data && <>
      <AgreementLessonPicker lessons={query.data} selected={lessonId} onSelect={setLessonId} optional disabled={mutation.busy} />
      <WorkspaceField label="Material title" value={title} onChangeText={setTitle} maxLength={200} editable={!mutation.busy} />
      <Text style={ui.body}>PDF / JPG / PNG · Up to 20 MB per file</Text>
      {asset && <Text style={ui.body}>{asset.name}</Text>}
      <WorkspaceButton label={fileLabel} disabled={mutation.busy} onPress={() => void mutation.run(chooseFile)} />
    </>}
    {!!mutation.error && <Text role="alert" style={ui.error}>{mutation.error}</Text>}
    <View style={ui.row}>
      <WorkspaceButton primary label="Upload material" disabled={mutation.busy || !query.data || !asset || !title.trim()}
        onPress={() => void mutation.run(submit)} />
      <WorkspaceButton label="Cancel" disabled={mutation.busy} onPress={back} />
    </View>
  </StudentPage>;
}
