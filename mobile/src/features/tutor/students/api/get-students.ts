import type { ApiClient } from '@/shared/api/api-client';

export type StudentAgreement = {
  tutoringAgreementId: string;
  title: string;
  subject: string;
  status: string;
  hourlyRate: number | null;
  currency: string | null;
};

export type Student = {
  studentId: string;
  displayName: string;
  email: string | null;
  agreements: StudentAgreement[];
};

export function getStudents(api: ApiClient, signal: AbortSignal) {
  return api.get<Student[]>('/api/tutors/me/students', { signal });
}

export function getPendingInvitations(api: ApiClient, signal: AbortSignal) {
  return api.get<{ invitationId: string; email: string; subject: string }[]>(
    '/api/tutors/me/student-invitations', { signal },
  );
}
