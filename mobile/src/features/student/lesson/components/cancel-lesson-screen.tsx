import { useCallback, useState } from 'react';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { Text, View } from 'react-native';
import type { ApiClient } from '@/shared/api/api-client';
import { useApi } from '@/shared/api/api-context';
import { WorkspaceButton, WorkspaceField, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { useWorkspaceQuery } from '@/shared/models/use-workspace-query';
import { useWorkspaceMutation } from '@/shared/models/use-workspace-mutation';
import { StudentPage, StudentQueryFeedback } from '../../workspace/components/student-workspace';
import { studentLessonRoute } from '../../workspace/models/student-routes';
import { shortDate, timeLabel } from '@/features/tutor/workspace/models/workspace-format';
import { getStudentLesson, cancelStudentLesson } from '../api/lessons';

export function CancelStudentLessonScreen() {
  const { lessonId } = useLocalSearchParams<{ lessonId: string }>();
  const router = useRouter();
  const api = useApi();
  const [reason, setReason] = useState('');
  const [confirming, setConfirming] = useState(false);
  const mutation = useWorkspaceMutation();
  const load = useCallback((client: ApiClient, signal: AbortSignal) => getStudentLesson(client, lessonId, signal), [lessonId]);
  const query = useWorkspaceQuery(load);
  async function cancel() {
    if (!reason.trim() || !confirming || query.data?.status !== 'Scheduled') return;
    await cancelStudentLesson(api, lessonId, reason.trim());
    router.replace(studentLessonRoute(lessonId));
  }
  return <StudentPage title="Cancel lesson">
    <StudentQueryFeedback {...query} />
    {query.data && <>
      <Text style={ui.body}>{query.data.subject} · {shortDate(query.data.startsAtUtc)} · {timeLabel(query.data.startsAtUtc)}</Text>
      {query.data.status !== 'Scheduled' && <Text style={ui.body}>This lesson is already {query.data.status.toLowerCase()}.</Text>}
      {query.data.status === 'Scheduled' && <>
        <WorkspaceField label="Cancellation reason" value={reason} multiline maxLength={500} editable={!mutation.busy && !confirming}
          onChangeText={setReason} />
        {confirming && <Text role="alert" style={ui.body}>Cancel this lesson? Your tutor will see this reason: {reason.trim()}</Text>}
        {!!mutation.error && <Text role="alert" style={ui.error}>{mutation.error}</Text>}
        <View style={ui.row}>
          {!confirming && <WorkspaceButton primary label="Review cancellation" disabled={!reason.trim()} onPress={() => setConfirming(true)} />}
          {confirming && <>
            <WorkspaceButton primary label="Confirm cancellation" disabled={mutation.busy} onPress={() => void mutation.run(cancel)} />
            <WorkspaceButton label="Edit reason" disabled={mutation.busy} onPress={() => setConfirming(false)} />
          </>}
        </View>
      </>}
    </>}
    <WorkspaceButton label="Keep lesson / Back" disabled={mutation.busy} onPress={() => router.replace(studentLessonRoute(lessonId))} />
  </StudentPage>;
}
