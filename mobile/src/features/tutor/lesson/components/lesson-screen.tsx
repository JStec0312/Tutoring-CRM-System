import { useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useRef, useState } from 'react';
import { Text, View } from 'react-native';
import { useApi } from '@/shared/api/api-context';
import type { ApiClient } from '@/shared/api/api-client';
import { WorkspaceButton, WorkspaceCard, WorkspaceField, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { getLesson, updateLesson } from '../api/lesson';
import { lessonSummary, lessonTime, shortDate } from '../../workspace/models/workspace-format';
import { MaterialList } from '../../materials/components/materials-screen';
import { getLessonCharges } from '../../payments/api/lesson-charges';
import { errorMessage, useTutorQuery } from '../../workspace/models/use-tutor-query';
import { agreementRoute } from '../../workspace/models/tutor-routes';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';

export function LessonScreen() {
  const { lessonId } = useLocalSearchParams<{ lessonId: string }>();
  const router = useRouter();
  const api = useApi();
  const load = useCallback(async (api: ApiClient, signal: AbortSignal) => {
    const lesson = await getLesson(api, lessonId, signal);
    return { ...lesson, canFinalize: lesson.status === 'Scheduled' && Date.parse(lesson.endsAtUtc) <= Date.now() };
  }, [lessonId]);
  const query = useTutorQuery(load);
  const lesson = query.data;
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);
  const [editingNote, setEditingNote] = useState(false);
  const [topic, setTopic] = useState('');
  const [coveredTopics, setCoveredTopics] = useState('');
  const [studentProgress, setStudentProgress] = useState('');
  const inFlight = useRef(false);
  async function update(property: 'status' | 'note', body: object) {
    if (inFlight.current) return;
    inFlight.current = true;
    setSaving(true);
    setError('');
    try {
      await updateLesson(api, lessonId, property, body);
      setEditingNote(false);
      query.reload();
    } catch (error) {
      setError(errorMessage(error));
    } finally {
      setSaving(false);
      inFlight.current = false;
    }
  }

  const title = lesson?.status === 'Completed' ? 'Completed lesson' : lesson?.status === 'Missed' ? 'Lesson marked missed'
    : lesson?.status === 'Cancelled' ? 'Lesson cancelled' : 'Lesson details';
  return <TutorPage title={title}>
    <QueryFeedback {...query} />
    {lesson && <>
      <WorkspaceCard title={`${shortDate(lesson.startsAtUtc)} · ${lessonTime(lesson)} · ${lesson.studentDisplayName}`}
        detail={lessonSummary(lesson)} />
      {lesson.status === 'Completed' && <LessonPaymentStatus key={lessonId} lessonId={lessonId} />}
      {lesson.plannedTopic && <WorkspaceCard title="Planned topic" detail={lesson.plannedTopic} />}
      {lesson.cancellationReason && <Text style={ui.body}>Reason · {lesson.cancellationReason}</Text>}
      {lesson.status === 'Scheduled' && <View style={ui.row}>
        <WorkspaceButton primary label="Edit lesson" disabled={saving} onPress={() => router.push({
          pathname: '/tutor/lessons/[lessonId]/edit', params: { lessonId },
        })} />
        <WorkspaceButton label="Cancel lesson" disabled={saving} onPress={() => router.push({
          pathname: '/tutor/lessons/[lessonId]/cancel', params: { lessonId },
        })} />
      </View>}
      {lesson.canFinalize && <View style={ui.row}>
        <WorkspaceButton primary label="Mark completed" disabled={saving}
          onPress={() => void update('status', { status: 'Completed' })} />
        <WorkspaceButton label="Mark missed" disabled={saving}
          onPress={() => void update('status', { status: 'Missed' })} />
      </View>}
      {lesson.note && <WorkspaceCard title={lesson.note.topic || 'Lesson note'}
        detail={[lesson.note.coveredTopics, lesson.note.studentProgress].filter(Boolean).join('\n')} />}
      {lesson.status !== 'Cancelled' && lesson.status !== 'Missed' && !editingNote && <WorkspaceButton label={lesson.note ? 'Edit lesson note' : 'Add lesson note'} onPress={() => {
        setTopic(lesson.note?.topic ?? lesson.plannedTopic ?? '');
        setCoveredTopics(lesson.note?.coveredTopics ?? '');
        setStudentProgress(lesson.note?.studentProgress ?? '');
        setEditingNote(true);
      }} />}
      {editingNote && <View style={ui.stack}>
        <WorkspaceField label="Topic" value={topic} onChangeText={setTopic} maxLength={200} editable={!saving} />
        <WorkspaceField label="Covered issues" value={coveredTopics} onChangeText={setCoveredTopics} maxLength={2000} multiline editable={!saving} />
        <WorkspaceField label="Progress note" value={studentProgress} onChangeText={setStudentProgress} maxLength={2000} multiline editable={!saving} />
        <WorkspaceButton primary label={saving ? 'Saving…' : 'Save lesson note'}
          disabled={saving || ![topic, coveredTopics, studentProgress].every(value => value.trim())}
          onPress={() => void update('note', { topic, coveredTopics, studentProgress })} />
        <WorkspaceButton label="Cancel" disabled={saving} onPress={() => setEditingNote(false)} />
      </View>}
      {lesson.status === 'Completed' && <WorkspaceButton label="Payments" onPress={() => router.push({
        pathname: '/tutor/payments', params: { lessonId },
      })} />}
      <Text accessibilityRole="header" style={ui.sectionHeading}>Attached materials</Text>
      <MaterialList agreementId={lesson.tutoringAgreementId} lessonId={lessonId} attachedOnly />
      <WorkspaceButton label="Attach material" onPress={() => router.push({
        pathname: '/tutor/materials', params: { agreementId: lesson.tutoringAgreementId, lessonId },
      })} />
      <WorkspaceButton label="Open agreement" onPress={() => router.push(agreementRoute(lesson.tutoringAgreementId))} />
    </>}
    {!!error && <Text accessibilityRole="alert" style={ui.error}>{error}</Text>}
    <WorkspaceButton label="Back" disabled={saving} onPress={() => {
      if (router.canGoBack()) router.back();
      else router.replace('/tutor/calendar');
    }} />
  </TutorPage>;
}

function LessonPaymentStatus({ lessonId }: { lessonId: string }) {
  const load = useCallback((api: ApiClient, signal: AbortSignal) =>
    getLessonCharges(api, signal, { lessonId }), [lessonId]);
  const query = useTutorQuery(load);
  if (query.error) return <View style={ui.row}>
    <Text accessibilityRole="alert" style={ui.error}>Payment status unavailable.</Text>
    <WorkspaceButton label="Retry payment status" onPress={query.reload} />
  </View>;

  const [charge] = query.data ?? [];
  const status = query.loading ? 'Loading…' : !charge ? 'No charge' : charge.paidAtUtc ? 'Paid' : 'Unpaid';
  return <Text accessibilityLiveRegion="polite" style={ui.body}>Payment · {status}</Text>;
}
