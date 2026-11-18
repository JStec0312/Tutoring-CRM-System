export function studentLessonRoute(lessonId: string) {
  return { pathname: '/student/lessons/[lessonId]' as const, params: { lessonId } };
}
