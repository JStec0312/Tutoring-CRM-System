import type { ApiClient } from '@/shared/api/api-client';

export type StudentAgreement = {
  agreementId: string;
  title: string;
  subject: string;
  status: string;
  tutorFirstName: string | null;
  tutorLastName: string | null;
  hourlyRate: number | null;
  currency: string | null;
};

export function getStudentAgreements(api: ApiClient, signal: AbortSignal) {
  return api.get<StudentAgreement[]>('/api/students/me/agreements', { signal });
}

export function tutorName(agreement: { tutorFirstName: string | null; tutorLastName: string | null }) {
  const name = [agreement.tutorFirstName, agreement.tutorLastName].filter(Boolean).join(' ');
  return name || 'Tutor';
}

export function agreementLabel(agreement: StudentAgreement) {
  return `${tutorName(agreement)} / ${agreement.subject}`;
}
