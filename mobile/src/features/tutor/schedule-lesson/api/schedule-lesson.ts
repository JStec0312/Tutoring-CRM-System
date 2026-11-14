import type { ApiClient } from '@/shared/api/api-client';

export function scheduleLesson(api: ApiClient, tutoringAgreementId: string, startsAt: Date, durationMinutes: number, topic: string) {
  return api.post<{ lessonId: string }>('/api/tutors/me/lessons', {
    tutoringAgreementId, startsAt: startsAt.toISOString(), durationMinutes, topic,
  });
}

export function rescheduleLesson(api: ApiClient, lessonId: string, startsAt: Date, durationMinutes: number, topic: string) {
  return api.put(`/api/tutors/me/lessons/${encodeURIComponent(lessonId)}/schedule`, {
    startsAt: startsAt.toISOString(), durationMinutes, topic,
  });
}
