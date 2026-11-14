import type { ApiClient } from '@/shared/api/api-client';

export type CalendarLesson = { lessonId: string; studentDisplayName: string; subject: string; startsAtUtc: string; endsAtUtc: string; status: string };
export type DashboardData = { completed: number; teachingSeconds: number; unpaidLessons: number; overdueAgreements: number; lessons: CalendarLesson[] };

export async function getTutorDashboard(api: ApiClient, now: Date, signal?: AbortSignal): Promise<DashboardData> {
  const start = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const end = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1);
  const query = `from=${encodeURIComponent(start.toISOString())}&to=${encodeURIComponent(end.toISOString())}`;
  const options = { signal };
  const [completed, teaching, overdue, calendar] = await Promise.all([
    api.get<{ completedLessonCount: number }>('/api/tutors/me/lessons/completed-count', options),
    api.get<{ totalTeachingSeconds: number }>('/api/tutors/me/lessons/total-teaching-time', options),
    api.get<{ unpaidLessonCount: number }[]>('/api/tutors/tutoring-agreements/overdue', options),
    api.get<CalendarLesson[]>(`/api/calendar?${query}`, options),
  ]);
  return { completed: completed.completedLessonCount, teachingSeconds: teaching.totalTeachingSeconds,
    unpaidLessons: overdue.reduce((sum, agreement) => sum + agreement.unpaidLessonCount, 0), overdueAgreements: overdue.length,
    lessons: calendar.filter(lesson => lesson.status === 'Scheduled' && Date.parse(lesson.endsAtUtc) > now.getTime()).sort((a, b) => Date.parse(a.startsAtUtc) - Date.parse(b.startsAtUtc)) };
}
