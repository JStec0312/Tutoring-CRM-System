import { Redirect, useLocalSearchParams } from 'expo-router';

export default function PaymentRedirect() {
  const { agreementId, lessonId } = useLocalSearchParams<{ agreementId?: string; lessonId?: string }>();
  return <Redirect href={{ pathname: '/tutor/payments', params: { agreementId, lessonId } }} />;
}
