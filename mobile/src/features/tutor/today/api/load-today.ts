import type { ApiClient } from '@/shared/api/api-client';
import { getCalendar } from '../../calendar/api/get-calendar';
import { getLessonsToClose } from '../../notifications/api/get-lessons-to-close';
import { getOverdueAgreements } from '../../payments/api/get-overdue-agreements';
import { addDays, startOfDay } from '../../workspace/models/workspace-format';

export async function loadToday(api: ApiClient, signal: AbortSignal, now = new Date()) {
  const start = startOfDay(now);
  const [calendar, toClose, overdue] = await Promise.all([
    getCalendar(api, start, addDays(start, 1), signal),
    getLessonsToClose(api, signal, now),
    getOverdueAgreements(api, signal),
  ]);
  return {
    upcoming: calendar.filter(lesson => lesson.status === 'Scheduled' && Date.parse(lesson.endsAtUtc) > now.getTime()),
    toClose,
    overdue,
  };
}
