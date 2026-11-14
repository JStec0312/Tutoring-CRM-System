import type { ApiClient } from '@/shared/api/api-client';

export type OverdueAgreement = {
  tutoringAgreementId: string;
  studentId: string;
  studentDisplayName: string;
  subject: string;
  unpaidLessonCount: number;
  amountsByCurrency: { currency: string; amount: number; unpaidLessonCount: number }[];
};

export function getOverdueAgreements(api: ApiClient, signal: AbortSignal) {
  return api.get<OverdueAgreement[]>('/api/tutors/tutoring-agreements/overdue', { signal });
}
