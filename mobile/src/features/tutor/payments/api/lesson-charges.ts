import type { ApiClient } from '@/shared/api/api-client';

export type LessonCharge = {
  chargeId: string;
  agreementId: string;
  lessonId: string;
  studentName: string;
  subject: string;
  startsAtUtc: string;
  endsAtUtc: string;
  amount: number;
  currency: string;
  paidAtUtc: string | null;
};

const endpoint = '/api/tutors/me/lesson-charges';

export function getLessonCharges(api: ApiClient, signal: AbortSignal, filters: { agreementId?: string; lessonId?: string } = {}) {
  const query = new URLSearchParams();
  if (filters.agreementId) query.set('agreementId', filters.agreementId);
  if (filters.lessonId) query.set('lessonId', filters.lessonId);
  return api.get<LessonCharge[]>(`${endpoint}?${query}`, { signal });
}

export function recordPayments(api: ApiClient, lessonChargeIds: string[]) {
  return api.post(`${endpoint}/mark-paid`, { lessonChargeIds, groupByAgreementAndCurrency: true });
}

export function markChargeUnpaid(api: ApiClient, chargeId: string) {
  return api.post(`${endpoint}/${encodeURIComponent(chargeId)}/mark-unpaid`);
}

export function chargeTotals(charges: LessonCharge[]) {
  const totals = new Map<string, number>();
  for (const { currency, amount } of charges) totals.set(currency, (totals.get(currency) ?? 0) + amount);
  return [...totals].sort(([a], [b]) => a.localeCompare(b));
}
