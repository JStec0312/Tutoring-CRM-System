import type { ApiClient } from '@/shared/api/api-client';

export type StudentLesson = {
  lessonId: string;
  tutoringAgreementId: string;
  subject: string;
  startsAtUtc: string;
  endsAtUtc: string;
  status: string;
  plannedTopic: string | null;
  cancellationReason: string | null;
};

export type StudentLessonDetails = StudentLesson & {
  tutorFirstName: string | null;
  tutorLastName: string | null;
  estimatedAmount: number | null;
  currency: string | null;
};

export function getStudentLessons(api: ApiClient, agreementId: string, signal: AbortSignal) {
  return api.get<StudentLesson[]>(`/api/students/me/agreements/${encodeURIComponent(agreementId)}/lessons`, { signal });
}

export function getStudentLesson(api: ApiClient, lessonId: string, signal: AbortSignal) {
  return api.get<StudentLessonDetails>(`/api/students/me/lessons/${encodeURIComponent(lessonId)}`, { signal });
}

export function cancelStudentLesson(api: ApiClient, lessonId: string, reason: string) {
  return api.post(`/api/lessons/${encodeURIComponent(lessonId)}/cancel`, { reason });
}
