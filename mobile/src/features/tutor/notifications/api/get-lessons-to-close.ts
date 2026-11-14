import type { ApiClient } from '@/shared/api/api-client';
import { getCalendar } from '../../calendar/api/get-calendar';

export function getLessonsToClose(api: ApiClient, signal: AbortSignal, now = new Date()) {
  return getCalendar(api, new Date('0001-01-01T00:00:00Z'), now, signal, true);
}
