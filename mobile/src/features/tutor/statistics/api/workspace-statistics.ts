import type { ApiClient } from '@/shared/api/api-client';
import { getCalendar } from '../../calendar/api/get-calendar';
import { getStudents } from '../../students/api/get-students';
import { getLessonCharges } from '../../payments/api/lesson-charges';

type Revenue = { currency: string; weekly: number; monthly: number; yearly: number };

export async function getWorkspaceStatistics(api: ApiClient, signal: AbortSignal) {
  const today = new Date();
  const earliest = new Date(Date.UTC(today.getUTCFullYear(), 0, -6));
  const end = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), today.getUTCDate() + 1));
  const [students, lessons, charges, revenue] = await Promise.all([
    getStudents(api, signal),
    getCalendar(api, earliest, end, signal),
    getLessonCharges(api, signal),
    api.get<{ revenueByCurrency: Revenue[] }>('/api/tutors/me/statistics/revenue', { signal }),
  ]);
  return { students, lessons, charges, revenue: revenue.revenueByCurrency };
}
