import { useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useRef, useState } from 'react';
import { Text, View } from 'react-native';
import { useApi } from '@/shared/api/api-context';
import type { ApiClient } from '@/shared/api/api-client';
import { WorkspaceButton, WorkspaceCard, WorkspaceField, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { cancelLesson, getLesson } from '../api/lesson';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';
import { lessonRoute } from '../../workspace/models/tutor-routes';
import { errorMessage, useTutorQuery } from '../../workspace/models/use-tutor-query';
import { lessonTime, shortDate } from '../../workspace/models/workspace-format';

export function CancelLessonScreen() {
  const { lessonId } = useLocalSearchParams<{ lessonId: string }>();
  const router = useRouter();
  const api = useApi();
  const load = useCallback((client: ApiClient, signal: AbortSignal) => getLesson(client, lessonId, signal), [lessonId]);
  const query = useTutorQuery(load);
  const [reason, setReason] = useState('');
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);
  const inFlight = useRef(false);
  const back = () => router.dismissTo(lessonRoute({ lessonId }));

  async function confirm() {
    if (inFlight.current) return;
    inFlight.current = true;
    setSaving(true);
    setError('');
    try {
      await cancelLesson(api, lessonId, reason.trim());
      back();
    } catch (error) {
      setError(errorMessage(error));
    } finally {
      inFlight.current = false;
      setSaving(false);
    }
  }

  const lesson = query.data;
  return <TutorPage title="Cancel lesson">
    <QueryFeedback {...query} />
    {lesson && <>
      <WorkspaceCard title={`${lesson.studentDisplayName} · ${lesson.subject}`}
        detail={`${shortDate(lesson.startsAtUtc)} · ${lessonTime(lesson)} · ${lesson.status}`} />
      {lesson.status === 'Scheduled' && <>
        <Text style={ui.body}>The student will see the updated schedule.</Text>
        <WorkspaceField label="Reason for cancellation" value={reason} onChangeText={setReason}
          maxLength={500} editable={!saving} />
      </>}
    </>}
    {!!error && <Text accessibilityRole="alert" style={ui.error}>{error}</Text>}
    <View style={ui.row}>
      {lesson?.status === 'Scheduled' && <WorkspaceButton primary label={saving ? 'Cancelling…' : 'Confirm cancellation'}
        disabled={saving || !reason.trim()} onPress={() => void confirm()} />}
      <WorkspaceButton label={lesson?.status === 'Scheduled' ? 'Keep lesson' : 'Back to lesson'} disabled={saving} onPress={back} />
    </View>
  </TutorPage>;
}
