import type { ApiClient } from '@/shared/api/api-client';
import type { StudentLesson } from '../../lesson/api/lessons';

export type StudentCalendarLesson = Omit<StudentLesson, 'plannedTopic' | 'cancellationReason'> & {
  tutorFirstName: string | null;
  tutorLastName: string | null;
};

export function getStudentCalendar(api: ApiClient, from: Date, to: Date, signal: AbortSignal) {
  const params = new URLSearchParams({ from: from.toISOString(), to: to.toISOString() });
  return api.get<StudentCalendarLesson[]>(`/api/calendar?${params}`, { signal });
}
