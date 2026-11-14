import type { ApiClient } from '@/shared/api/api-client';

export type CalendarLesson = {
  lessonId: string;
  tutoringAgreementId: string;
  studentDisplayName: string;
  title: string;
  subject: string;
  startsAtUtc: string;
  endsAtUtc: string;
  status: string;
  estimatedAmount: number | null;
  currency: string | null;
};

export function getCalendar(api: ApiClient, from: Date, to: Date, signal: AbortSignal, awaitingOutcome = false) {
  const query = new URLSearchParams({ from: from.toISOString(), to: to.toISOString(), tutorOnly: 'true' });
  if (awaitingOutcome) query.set('awaitingOutcome', 'true');
  return api.get<CalendarLesson[]>(`/api/calendar?${query}`, { signal });
}
