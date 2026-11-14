import type { ApiClient } from '@/shared/api/api-client';
import type { CalendarLesson } from '../../calendar/api/get-calendar';

export function getAgreementLessons(api: ApiClient, agreementId: string, signal: AbortSignal) {
  const query = new URLSearchParams({
    from: '0001-01-01T00:00:00Z', to: '9999-12-31T23:59:59Z', tutorOnly: 'true', agreementId,
  });
  return api.get<CalendarLesson[]>(`/api/calendar?${query}`, { signal });
}
