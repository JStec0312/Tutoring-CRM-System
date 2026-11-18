import { router } from "expo-router";
import { useRef, useState } from "react";
import { Text } from "react-native";

import { useApi } from "@/shared/api/api-context";
import { useAuth } from "@/shared/session/AuthProvider";
import { FormError } from "@/shared/ui/FormError";
import { FormScreen, FormSubmit, formStyles } from "@/shared/ui/FormScreen";
import { FormTextButton } from "@/shared/ui/FormTextButton";
import { PasswordSignInForm } from "@/shared/ui/PasswordSignInForm";
import { completeGoogleAccountLink } from "../api/complete-google-account-link";
import { useGoogleContinuation } from "../../sign-in/models/GoogleContinuationProvider";
import { isGoogleContinuationExpired } from "../../sign-in/models/google-continuation";
import { getGoogleLinkError } from "../models/google-link-error";

export function GoogleLinkScreen() {
  const api = useApi();
  const { isAuthenticated, signIn } = useAuth();
  const { continuation, setContinuation } = useGoogleContinuation();
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const submitting = useRef(false);

  function leaveLinking() {
    if (pending || submitting.current) return;

    setContinuation(null);
    if (isAuthenticated) {
      router.replace("/");
    } else {
      router.replace("/sign-in");
    }
  }

  async function handleConfirm() {
    if (pending || submitting.current || !continuation || !isAuthenticated) return;

    submitting.current = true;
    setPending(true);
    setError(null);
    try {
      await completeGoogleAccountLink(api, continuation);
      setContinuation(null);
      router.replace("/");
    } catch (error) {
      setError(getGoogleLinkError(error));
    } finally {
      submitting.current = false;
      setPending(false);
    }
  }

  if (!continuation || continuation.status !== "linkRequired" || !continuation.email || isGoogleContinuationExpired(continuation)) {
    return (
      <FormScreen title="Google sign-in">
        <FormError message="This linking attempt has expired or is no longer available. Start Google sign-in again." />
        <FormTextButton label="Continue" disabled={pending} onPress={leaveLinking} />
      </FormScreen>
    );
  }

  return (
    <FormScreen title="Connect your Google account">
      {!isAuthenticated && (
        <>
          <Text style={formStyles.description}>
            Enter the password for your CRM account: {continuation.email}
          </Text>
          <PasswordSignInForm
            pending={pending}
            onPendingChange={setPending}
            onSubmit={signIn}
            submitLabel="Sign in to continue"
            fixedEmail={continuation.email}
          />
        </>
      )}
      {isAuthenticated && (
        <>
          <Text aria-live="polite" style={formStyles.description}>
            Connect the Google account you just selected to the CRM account you are signed in to?
          </Text>
          <FormSubmit label="Connect Google account" pending={pending} onPress={handleConfirm} />
        </>
      )}
      <FormError message={error} />
      <FormTextButton label="Cancel" disabled={pending} onPress={leaveLinking} />
    </FormScreen>
  );
}
