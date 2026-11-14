import { parseRate } from '../../workspace/models/workspace-format';

export function validateStudentForm(
  student: { mode: 'manual'; displayName: string } | { mode: 'invite'; email: string },
  title: string,
  subject: string,
  rate: string,
) {
  const hourlyRate = parseRate(rate);
  if (student.mode === 'manual' && !student.displayName.trim()) {
    return { error: 'Enter the student name.' } as const;
  }
  if (student.mode === 'invite' && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(student.email.trim())) {
    return { error: 'Enter a valid student email address.' } as const;
  }
  if (!title.trim() || !subject.trim() || Number.isNaN(hourlyRate)) {
    return { error: 'Enter the agreement title and subject, and a valid hourly rate if applicable.' } as const;
  }
  return { agreement: { title: title.trim(), subject: subject.trim(), hourlyRate } } as const;
}
