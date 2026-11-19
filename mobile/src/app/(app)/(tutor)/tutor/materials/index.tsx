import { Redirect, useLocalSearchParams } from 'expo-router';

export default function LegacyMaterialsRoute() {
  const { lessonId, agreementId } = useLocalSearchParams<{ lessonId?: string; agreementId?: string }>();
  if (lessonId) return <Redirect href={{ pathname: '/tutor/lessons/[lessonId]', params: { lessonId } }} />;
  return <Redirect href={{ pathname: '/tutor/lessons', params: { agreementId } }} />;
}
