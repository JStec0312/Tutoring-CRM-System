import type { Href } from 'expo-router';

export function studentRoute(studentId: string): Href {
  return { pathname: '/tutor/students/[studentId]', params: { studentId } };
}

export function agreementRoute(agreementId: string): Href {
  return { pathname: '/tutor/agreements/[agreementId]', params: { agreementId } };
}

export function lessonRoute(lesson: { lessonId: string }): Href {
  return { pathname: '/tutor/lessons/[lessonId]', params: { lessonId: lesson.lessonId } };
}
