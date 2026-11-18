import type { ApiClient } from '@/shared/api/api-client';
import { getStudentCalendar } from '../../calendar/api/get-student-calendar';
import { addDays } from '@/features/tutor/workspace/models/workspace-format';

export async function getStudentNotifications(api: ApiClient, signal: AbortSignal, now = new Date()) {
  const lessons = await getStudentCalendar(api, addDays(now, -7), addDays(now, 7), signal);
  return lessons.filter(lesson => lesson.status === 'Cancelled'
    || (lesson.status === 'Scheduled' && Date.parse(lesson.startsAtUtc) >= now.getTime()));
}
