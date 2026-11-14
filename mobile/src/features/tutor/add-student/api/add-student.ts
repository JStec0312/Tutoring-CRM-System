import type { ApiClient } from '@/shared/api/api-client';

type AgreementInput = { title: string; subject: string; hourlyRate: number | null };

export function addStudent(api: ApiClient, input: AgreementInput & { displayName: string }) {
  return api.post<{ studentId: string; agreementId: string }>('/api/tutors/me/students', input);
}

export function inviteStudent(api: ApiClient, input: AgreementInput & { email: string }) {
  return api.post('/api/tutors/me/student-invitations', input);
}
