import { Redirect, useLocalSearchParams } from 'expo-router';

export default function LegacyMaterialsRoute() {
  const { lessonId, agreementId } = useLocalSearchParams<{ lessonId?: string; agreementId?: string }>();
  if (lessonId) return <Redirect href={{ pathname: '/student/lessons/[lessonId]', params: { lessonId } }} />;
  return <Redirect href={{ pathname: '/student/lessons', params: { agreementId } }} />;
}
