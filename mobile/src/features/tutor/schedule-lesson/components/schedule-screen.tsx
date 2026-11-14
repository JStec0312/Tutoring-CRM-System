import { useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useRef, useState } from 'react';
import { Text, View } from 'react-native';
import { useApi } from '@/shared/api/api-context';
import type { ApiClient } from '@/shared/api/api-client';
import { WorkspaceButton, WorkspaceCard, WorkspaceField, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { rescheduleLesson, scheduleLesson } from '../api/schedule-lesson';
import { getLesson } from '../../lesson/api/lesson';
import { getAgreementOverview } from '../../agreement/api/agreement';
import { getStudents } from '../../students/api/get-students';
import { dateKey, lessonMinutes, money, parseLocalDateTime, timeLabel } from '../../workspace/models/workspace-format';
import { lessonRoute } from '../../workspace/models/tutor-routes';
import { errorMessage, useTutorQuery } from '../../workspace/models/use-tutor-query';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';

export function ScheduleScreen() {
  const { agreementId: initialAgreement, date: initialDate } = useLocalSearchParams<{ agreementId?: string; date?: string }>();
  return <LessonForm initialAgreement={initialAgreement} initialDate={initialDate} />;
}

export function EditLessonScreen() {
  const { lessonId } = useLocalSearchParams<{ lessonId: string }>();
  const router = useRouter();
  const load = useCallback(async (api: ApiClient, signal: AbortSignal) => {
    const lesson = await getLesson(api, lessonId, signal);
    const agreement = await getAgreementOverview(api, lesson.tutoringAgreementId, signal);
    return { lesson, agreement };
  }, [lessonId]);
  const query = useTutorQuery(load);
  if (query.data?.lesson.status === 'Scheduled') return <LessonForm key={lessonId} edit={query.data} />;

  return <TutorPage title="Edit lesson">
    <QueryFeedback {...query} />
    {query.data && <Text style={ui.body}>Only scheduled lessons can be edited.</Text>}
    <WorkspaceButton label="Back to lesson" onPress={() => router.dismissTo(lessonRoute({ lessonId }))} />
  </TutorPage>;
}

type EditLesson = {
  lesson: Awaited<ReturnType<typeof getLesson>>;
  agreement: Awaited<ReturnType<typeof getAgreementOverview>>;
};

function LessonForm({ initialAgreement, initialDate, edit }: {
  initialAgreement?: string;
  initialDate?: string;
  edit?: EditLesson;
}) {
  const router = useRouter();
  const api = useApi();
  const query = useTutorQuery(getStudents);
  const [agreementId, setAgreementId] = useState(initialAgreement);
  const [date, setDate] = useState(edit ? dateKey(new Date(edit.lesson.startsAtUtc)) : initialDate ?? dateKey(new Date()));
  const [time, setTime] = useState(edit ? timeLabel(edit.lesson.startsAtUtc) : '');
  const [duration, setDuration] = useState(edit ? String(lessonMinutes(edit.lesson)) : '60');
  const [topic, setTopic] = useState(edit?.lesson.plannedTopic ?? '');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const inFlight = useRef(false);
  const agreements = query.data?.flatMap(student => student.agreements
    .filter(agreement => agreement.status === 'Active')
    .map(agreement => ({ ...agreement, studentName: student.displayName })));
  const selectedId = edit?.lesson.tutoringAgreementId ?? agreementId ??
    (agreements?.length === 1 ? agreements[0].tutoringAgreementId : undefined);
  const selected = edit?.agreement ?? agreements?.find(agreement => agreement.tutoringAgreementId === selectedId);
  const rate = selected?.hourlyRate;
  const currency = selected?.currency;
  function back() {
    if (edit) router.dismissTo(lessonRoute(edit.lesson));
    else if (router.canGoBack()) router.back();
    else router.replace('/tutor/calendar');
  }

  async function submit() {
    if (inFlight.current) return;
    const startsAt = parseLocalDateTime(date.trim(), time.trim());
    const durationMinutes = Number(duration);
    if (!selectedId || !selected) {
      setError('Select an active agreement.');
      return;
    }
    if (!startsAt || !/^\d+$/.test(duration.trim()) || !Number.isSafeInteger(durationMinutes) || durationMinutes < 1 || durationMinutes > 2147483647) {
      setError('Enter a valid date, a 24-hour start time and a positive duration in minutes.');
      return;
    }
    inFlight.current = true;
    setSaving(true);
    setError('');
    try {
      if (edit) {
        await rescheduleLesson(api, edit.lesson.lessonId, startsAt, durationMinutes, topic.trim());
        back();
      } else {
        const lesson = await scheduleLesson(api, selectedId, startsAt, durationMinutes, topic.trim());
        router.replace(lessonRoute(lesson));
      }
    } catch (error) {
      setError(errorMessage(error));
    } finally {
      inFlight.current = false;
      setSaving(false);
    }
  }

  return <TutorPage title={edit ? 'Edit lesson' : 'Schedule lesson'}>
    {edit ? <WorkspaceCard title="Tutoring agreement" detail={`${edit.lesson.studentDisplayName} · ${edit.lesson.title}`} /> : <>
      <QueryFeedback {...query} />
      <Text style={ui.sectionHeading}>Tutoring agreement</Text>
      {agreements?.length === 0 && <Text style={ui.body}>Add or activate a tutoring agreement to schedule a lesson.</Text>}
      {agreements?.map(agreement => <WorkspaceButton key={agreement.tutoringAgreementId}
      label={`${agreement.studentName} · ${agreement.subject} · ${agreement.title}`}
      primary={selectedId === agreement.tutoringAgreementId} disabled={saving}
      onPress={() => setAgreementId(agreement.tutoringAgreementId)} />)}
    </>}
    <WorkspaceField label="Date (YYYY-MM-DD)" value={date} onChangeText={setDate} editable={!saving} />
    <WorkspaceField label="Start time (HH:MM)" value={time} onChangeText={setTime} editable={!saving} />
    <WorkspaceField label="Duration in minutes" keyboardType="numeric" value={duration} onChangeText={setDuration} editable={!saving} />
    <WorkspaceField label="Topic (optional)" value={topic} onChangeText={setTopic} maxLength={200} editable={!saving} />
    {rate != null && currency && /^\d+$/.test(duration) && Number(duration) > 0 &&
      <WorkspaceCard title="Estimated cost" detail={`${money(rate * Number(duration) / 60, currency)} · ${money(rate, currency)}/hour`} />}
    <Text style={ui.body}>Times are shown in your local time zone.</Text>
    {!!error && <Text accessibilityRole="alert" style={ui.error}>{error}</Text>}
    <View style={ui.row}>
      <WorkspaceButton primary label={saving ? 'Saving…' : 'Save lesson'} disabled={saving || !selected}
        onPress={() => void submit()} />
      <WorkspaceButton label="Cancel" disabled={saving} onPress={back} />
    </View>
  </TutorPage>;
}
