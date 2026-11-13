import { useLocalSearchParams } from "expo-router";

import { ResetPasswordScreen } from "@/features/auth/reset-password/components/ResetPasswordScreen";

export default function ResetPasswordRoute() {
  const { token } = useLocalSearchParams<{ token?: string | string[] }>();
  const resetToken = typeof token === "string" ? token.trim() : "";

  return <ResetPasswordScreen key={resetToken} token={resetToken} />;
}
