import { useRouter } from 'expo-router';
import { Text } from 'react-native';
import { WorkspaceButton, WorkspaceCard, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import type { ApiClient } from '@/shared/api/api-client';
import { getCalendar } from '../../calendar/api/get-calendar';
import { getLessonsToClose } from '../api/get-lessons-to-close';
import { addDays, lessonSummary, lessonTime, shortDate } from '../../workspace/models/workspace-format';
import { useTutorQuery } from '../../workspace/models/use-tutor-query';
import { lessonRoute } from '../../workspace/models/tutor-routes';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';

async function getNotifications(api: ApiClient, signal: AbortSignal) {
  const now = new Date();
  const [toClose, recent] = await Promise.all([
    getLessonsToClose(api, signal, now),
    getCalendar(api, addDays(now, -7), addDays(now, 1), signal),
  ]);
  return {
    toClose,
    upcoming: recent.filter(lesson => lesson.status === 'Scheduled' && Date.parse(lesson.startsAtUtc) > now.getTime()),
    cancelled: recent.filter(lesson => lesson.status === 'Cancelled'),
  };
}

export function NotificationsScreen() {
  const router = useRouter();
  const query = useTutorQuery(getNotifications);
  return <TutorPage title="Notifications">
    <QueryFeedback {...query} />
    {query.data && !query.data.upcoming.length && !query.data.cancelled.length && !query.data.toClose.length &&
      <Text style={ui.body}>No lesson notifications.</Text>}
    {query.data?.upcoming.map(lesson => <WorkspaceCard key={lesson.lessonId} title="Lesson reminder"
      detail={`${lesson.studentDisplayName} · ${lesson.subject} · ${shortDate(lesson.startsAtUtc)} · ${lessonTime(lesson)}`}
      onPress={() => router.push(lessonRoute(lesson))} />)}
    {query.data?.cancelled.map(lesson => <WorkspaceCard key={lesson.lessonId} title="Lesson cancelled"
      detail={`${lesson.studentDisplayName} · ${lesson.subject} · ${shortDate(lesson.startsAtUtc)}`}
      onPress={() => router.push(lessonRoute(lesson))} />)}
    {!!query.data?.toClose.length && <Text style={ui.sectionHeading}>Lessons awaiting an outcome</Text>}
    {query.data?.toClose.map(lesson => <WorkspaceCard key={lesson.lessonId}
      title={`${shortDate(lesson.startsAtUtc)} · ${lessonTime(lesson)} · ${lesson.studentDisplayName}`}
      detail={lessonSummary(lesson)} onPress={() => router.push(lessonRoute(lesson))} />)}
    <WorkspaceButton label="Back to today" onPress={() => router.navigate('/tutor')} />
  </TutorPage>;
}
