import { router } from "expo-router";
import { useState } from "react";
import { Text } from "react-native";

import { FormError } from "@/shared/ui/FormError";
import { FormScreen, formStyles } from "@/shared/ui/FormScreen";
import { FormTextButton } from "@/shared/ui/FormTextButton";
import { useGoogleContinuation } from "../../sign-in/models/GoogleContinuationProvider";
import { isGoogleContinuationExpired } from "../../sign-in/models/google-continuation";
import { GoogleRegistrationForm } from "./GoogleRegistrationForm";

export function GoogleRegistrationScreen() {
  const { continuation, setContinuation } = useGoogleContinuation();
  const [pending, setPending] = useState(false);

  function handleCancel() {
    if (pending) return;
    setContinuation(null);
    router.replace("/sign-in");
  }

  function handleRegistered() {
    setContinuation(null);
    router.replace("/");
  }

  if (!continuation || continuation.status !== "registrationRequired" || isGoogleContinuationExpired(continuation)) {
    return (
      <FormScreen title="Create your account">
        <FormError message="This registration attempt has expired or is no longer available. Start Google sign-in again." />
        <FormTextButton label="Back to sign in" disabled={pending} onPress={handleCancel} />
      </FormScreen>
    );
  }

  return (
    <FormScreen title="Create your account">
      <Text style={formStyles.description}>Choose your account type and username to continue with Google.</Text>
      <GoogleRegistrationForm
        continuation={continuation} pending={pending} onPendingChange={setPending}
        onRegistered={handleRegistered} onCancel={handleCancel}
      />
    </FormScreen>
  );
}
