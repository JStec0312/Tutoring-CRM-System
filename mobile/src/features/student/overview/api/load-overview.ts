import type { ApiClient } from '@/shared/api/api-client';
import { getStudentAgreements } from '../../tutors/api/agreements';
import { getStudentCalendar } from '../../calendar/api/get-student-calendar';
import { getStudentCharges } from '../../payments/api/charges';
import { addDays } from '@/features/tutor/workspace/models/workspace-format';

export async function loadStudentOverview(api: ApiClient, signal: AbortSignal, now = new Date()) {
  const [agreements, calendar] = await Promise.all([
    getStudentAgreements(api, signal), getStudentCalendar(api, now, addDays(now, 365), signal),
  ]);
  const summaries = await Promise.all(agreements.map(async agreement => {
    const charges = await getStudentCharges(api, agreement.agreementId, signal);
    const next = calendar.find(lesson => lesson.tutoringAgreementId === agreement.agreementId && lesson.status === 'Scheduled');
    return { agreement, charges, next };
  }));
  const next = calendar.find(lesson => lesson.status === 'Scheduled');
  return { summaries, next };
}
