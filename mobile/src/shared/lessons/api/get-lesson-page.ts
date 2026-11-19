import type { ApiClient } from '@/shared/api/api-client';

export type LessonView = 'all' | 'upcoming' | 'past';
export type LessonChargeSummary = { chargeId: string; amount: number; currency: string; paidAtUtc: string | null };
export type LessonListItem = {
  lessonId: string;
  tutoringAgreementId: string;
  subject: string;
  plannedTopic: string | null;
  startsAtUtc: string;
  endsAtUtc: string;
  status: string;
  cancellationReason: string | null;
  studentDisplayName: string;
  tutorFirstName: string | null;
  tutorLastName: string | null;
  charges: LessonChargeSummary[];
};
export type LessonPage = { items: LessonListItem[]; page: number; pageSize: number; totalCount: number };

export async function getLessonPage(api: ApiClient, options: {
  tutorOnly: boolean; page: number; view: LessonView; agreementId?: string;
}, signal: AbortSignal) {
  const pageSize = Number(process.env.EXPO_PUBLIC_LESSONS_PAGE_SIZE);
  if (!Number.isInteger(pageSize) || pageSize < 1 || pageSize > 50) {
    throw new Error('Set EXPO_PUBLIC_LESSONS_PAGE_SIZE to an integer from 1 to 50.');
  }
  const params = new URLSearchParams({ page: String(options.page), pageSize: String(pageSize), view: options.view, tutorOnly: String(options.tutorOnly) });
  if (options.agreementId) params.set('agreementId', options.agreementId);
  return api.get<LessonPage>(`/api/lessons?${params}`, { signal });
}
