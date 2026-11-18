import { Link, router } from "expo-router";
import { useRef, useState } from "react";
import { Platform, Text, View } from "react-native";

import { useApi } from "@/shared/api/api-context";
import { useAuth } from "@/shared/session/AuthProvider";
import type { LoginCredentials } from "@/shared/session/session-api";
import { FormError } from "@/shared/ui/FormError";
import { formStyles } from "@/shared/ui/FormScreen";
import { PasswordSignInForm } from "@/shared/ui/PasswordSignInForm";
import { useGoogleLink } from "../../google-link/models/GoogleLinkProvider";
import type { GoogleLinkContinuation } from "../../google-link/models/google-link-continuation";
import { signInWithGoogle } from "../api/sign-in-with-google";
import { requestGoogleIdToken } from "../services/request-google-id-token";
import { getGoogleSignInError } from "../models/google-sign-in-error";
import { GoogleSignInButton } from "./GoogleSignInButton";

export function SignInForm() {
  const api = useApi();
  const { signIn, signInWith } = useAuth();
  const { setContinuation } = useGoogleLink();
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const submitting = useRef(false);

  function handlePendingChange(value: boolean) {
    submitting.current = value;
    setPending(value);
  }

  async function handlePasswordSignIn(credentials: LoginCredentials) {
    setNotice(null);
    setError(null);
    setContinuation(null);
    await signIn(credentials);
  }

  async function handleGoogleSignIn() {
    if (submitting.current) return;

    setNotice(null);
    setContinuation(null);
    handlePendingChange(true);
    setError(null);
    try {
      const idToken = await requestGoogleIdToken(api);
      if (!idToken) return;

      let linkContinuation: GoogleLinkContinuation | null = null;
      await signInWith(async () => {
        const response = await signInWithGoogle(api, idToken, Platform.OS !== "web");

        if (response.status === "signedIn") {
          return response.session;
        }

        if (response.status === "linkRequired") {
          linkContinuation = response;
        } else {
          setNotice("No CRM account is connected to this Google account. Create an account to continue.");
        }

        return null;
      });

      if (linkContinuation) {
        setContinuation(linkContinuation);
        router.replace("/link-google");
      }
    } catch (error) {
      setError(getGoogleSignInError(error));
    } finally {
      handlePendingChange(false);
    }
  }

  return (
    <>
      <PasswordSignInForm pending={pending} onPendingChange={handlePendingChange} onSubmit={handlePasswordSignIn} />
      <FormError message={error} />
      <GoogleSignInButton pending={pending} onPress={handleGoogleSignIn} />
      {notice && <Text accessibilityRole="status" style={formStyles.description}>{notice}</Text>}
      {!pending && (
        <View>
          <Link href="/forgot-password" style={formStyles.link}>Forgot password?</Link>
          <Link href="/register" style={formStyles.link}>Create an account</Link>
        </View>
      )}
    </>
  );
}
