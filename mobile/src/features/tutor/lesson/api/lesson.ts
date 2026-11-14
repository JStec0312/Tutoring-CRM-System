import type { ApiClient } from '@/shared/api/api-client';
import type { CalendarLesson } from '../../calendar/api/get-calendar';

const endpoint = '/api/tutors/me/lessons';

export async function getLesson(api: ApiClient, id: string, signal: AbortSignal) {
  const result = await api.get<{
    lesson: CalendarLesson;
    note: { topic: string; coveredTopics: string; studentProgress: string } | null;
    cancellationReason: string | null;
    plannedTopic: string | null;
  }>(`${endpoint}/${encodeURIComponent(id)}`, { signal });
  return { ...result.lesson, note: result.note, cancellationReason: result.cancellationReason, plannedTopic: result.plannedTopic };
}

export function cancelLesson(api: ApiClient, id: string, reason: string) {
  return api.post(`/api/lessons/${encodeURIComponent(id)}/cancel`, { reason });
}

export function updateLesson(api: ApiClient, id: string, property: 'status' | 'note', body: object) {
  return api.put(`${endpoint}/${encodeURIComponent(id)}/${property}`, body);
}
