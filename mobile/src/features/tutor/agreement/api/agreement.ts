import type { ApiClient } from '@/shared/api/api-client';
import type { CalendarLesson } from '../../calendar/api/get-calendar';

export type AgreementOverview = {
  studentId: string;
  studentDisplayName: string;
  subject: string;
  status: string;
  hourlyRate: number | null;
  currency: string | null;
  notes: string | null;
  nextLesson: CalendarLesson | null;
  materialCount: number;
};

type LessonHistoryItem = {
  lessonId: string;
  tutoringAgreementId: string;
  subject: string;
  topic: string | null;
  agreementTitle: string;
  startsAtUtc: string;
  endsAtUtc: string;
  status: string;
};

const agreementsEndpoint = '/api/tutors/tutoring-agreements';

export function getAgreementOverview(api: ApiClient, id: string, signal: AbortSignal) {
  return api.get<AgreementOverview>(`${agreementsEndpoint}/${encodeURIComponent(id)}/overview`, { signal });
}

export function getStudentHistory(api: ApiClient, id: string, signal: AbortSignal) {
  return api.get<LessonHistoryItem[]>(`/api/tutors/students/${encodeURIComponent(id)}/lessons/history`, { signal });
}

export function updateAgreement(api: ApiClient, id: string, property: 'details' | 'hourly-rate' | 'status', body: object) {
  return api.put(`${agreementsEndpoint}/${encodeURIComponent(id)}/${property}`, body);
}
