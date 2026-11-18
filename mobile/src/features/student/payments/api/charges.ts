import type { ApiClient } from '@/shared/api/api-client';

export type StudentCharge = {
  chargeId: string;
  lessonId: string;
  startsAtUtc: string;
  amount: number;
  currency: string;
  paidAtUtc: string | null;
};

export function getStudentCharges(api: ApiClient, agreementId: string, signal: AbortSignal) {
  return api.get<StudentCharge[]>(`/api/students/me/agreements/${encodeURIComponent(agreementId)}/charges`, { signal });
}

export function outstandingByCurrency(charges: StudentCharge[]) {
  const totals = new Map<string, number>();
  for (const charge of charges) {
    if (!charge.paidAtUtc) {
      totals.set(charge.currency, (totals.get(charge.currency) ?? 0) + charge.amount);
    }
  }
  return Array.from(totals).sort(([first], [second]) => first.localeCompare(second));
}
