import { getDocumentAsync, type DocumentPickerAsset } from 'expo-document-picker';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useState } from 'react';
import { Text, View } from 'react-native';
import { useApi } from '@/shared/api/api-context';
import type { ApiClient } from '@/shared/api/api-client';
import { WorkspaceButton, WorkspaceCard, WorkspaceField, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { getAgreementOverview } from '../../agreement/api/agreement';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';
import { useTutorMutation } from '../../workspace/models/use-tutor-mutation';
import { useTutorQuery } from '../../workspace/models/use-tutor-query';
import { getAgreementLessons } from '../api/get-agreement-lessons';
import { uploadMaterial } from '../api/materials';
import { AgreementLessonPicker } from './agreement-lesson-picker';

export function UploadMaterialScreen() {
  const { agreementId, lessonId: initialLesson } = useLocalSearchParams<{ agreementId?: string; lessonId?: string }>();
  const router = useRouter();
  const api = useApi();
  const [lessonId, setLessonId] = useState(initialLesson);
  const [title, setTitle] = useState('');
  const [asset, setAsset] = useState<DocumentPickerAsset>();
  const mutation = useTutorMutation();
  const load = useCallback(async (client: ApiClient, signal: AbortSignal) => {
    if (!agreementId) return undefined;
    const [agreement, lessons] = await Promise.all([
      getAgreementOverview(client, agreementId, signal), getAgreementLessons(client, agreementId, signal),
    ]);
    return { agreement, lessons };
  }, [agreementId]);
  const query = useTutorQuery(load);

  function back() {
    if (router.canGoBack()) router.back();
    else router.replace({ pathname: '/tutor/materials', params: { agreementId } });
  }

  async function chooseFile() {
    const result = await getDocumentAsync({
      type: ['application/pdf', 'image/jpeg', 'image/png'], base64: false,
    });
    if (result.canceled) return;
    const picked = result.assets[0];
    setAsset(undefined);
    if (!/\.(pdf|jpg|png)$/i.test(picked.name)) {
      mutation.setError('Choose a PDF, JPG or PNG file.');
      return;
    }
    if (picked.size != null && (picked.size > 20_000_000 || picked.size === 0)) {
      mutation.setError(picked.size === 0 ? 'Choose a file that is not empty.' : 'File is too large. Choose a file up to 20 MB.');
      return;
    }
    setAsset(picked);
  }

  async function submit() {
    if (!asset || !agreementId || !query.data || !title.trim()) return;
    const selectedLesson = query.data.lessons.find(lesson => lesson.lessonId === lessonId);
    if (lessonId && !selectedLesson) {
      mutation.setError('Choose a lesson in this agreement or clear the selection.');
      return;
    }
    await uploadMaterial(api, agreementId, title.trim(), asset, selectedLesson?.lessonId);
    back();
  }

  return <TutorPage title="Upload learning material">
    <QueryFeedback {...query} />
    {!agreementId && <Text style={ui.body}>Choose an agreement from Materials to upload a file.</Text>}
    {query.data && <>
      <WorkspaceCard title="Tutoring agreement" detail={`${query.data.agreement.studentDisplayName} · ${query.data.agreement.subject}`} />
      <AgreementLessonPicker lessons={query.data.lessons} selected={lessonId} onSelect={setLessonId} disabled={mutation.busy} optional />
      <WorkspaceField label="Material title" value={title} onChangeText={setTitle} maxLength={200} editable={!mutation.busy} />
      <WorkspaceCard title={asset?.name ?? 'PDF / JPG / PNG'}
        detail={asset?.size != null ? `${(asset.size / 1000).toFixed(0)} KB` : 'Up to 20 MB per file'} />
      <WorkspaceButton label={asset ? 'Choose another file' : 'Choose file'} disabled={mutation.busy}
        onPress={() => void mutation.run(chooseFile)} />
    </>}
    {!!mutation.error && <Text accessibilityRole="alert" style={ui.error}>{mutation.error}</Text>}
    <View style={ui.row}>
      <WorkspaceButton primary label="Upload material" disabled={mutation.busy || !query.data || !asset || !title.trim()}
        onPress={() => void mutation.run(submit)} />
      <WorkspaceButton label="Cancel" disabled={mutation.busy} onPress={back} />
    </View>
  </TutorPage>;
}
