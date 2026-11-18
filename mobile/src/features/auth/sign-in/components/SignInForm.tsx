import { Link, router } from "expo-router";
import { useRef, useState } from "react";
import { Platform, View } from "react-native";

import { useApi } from "@/shared/api/api-context";
import { useAuth } from "@/shared/session/AuthProvider";
import type { LoginCredentials } from "@/shared/session/session-api";
import { FormError } from "@/shared/ui/FormError";
import { formStyles } from "@/shared/ui/FormScreen";
import { PasswordSignInForm } from "@/shared/ui/PasswordSignInForm";
import { useGoogleContinuation } from "../models/GoogleContinuationProvider";
import type { GoogleContinuation } from "../models/google-continuation";
import { signInWithGoogle } from "../api/sign-in-with-google";
import { requestGoogleIdToken } from "../services/request-google-id-token";
import { getGoogleSignInError } from "../models/google-sign-in-error";
import { GoogleSignInButton } from "./GoogleSignInButton";

export function SignInForm() {
  const api = useApi();
  const { signIn, signInWith } = useAuth();
  const { setContinuation } = useGoogleContinuation();
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const submitting = useRef(false);

  function handlePendingChange(value: boolean) {
    submitting.current = value;
    setPending(value);
  }

  async function handlePasswordSignIn(credentials: LoginCredentials) {
    setError(null);
    setContinuation(null);
    await signIn(credentials);
  }

  async function handleGoogleSignIn() {
    if (submitting.current) return;

    setContinuation(null);
    handlePendingChange(true);
    setError(null);
    try {
      const idToken = await requestGoogleIdToken(api);
      if (!idToken) return;

      let continuation: GoogleContinuation | null = null;
      let nextRoute: "/link-google" | "/register-google" | null = null;
      await signInWith(async () => {
        const response = await signInWithGoogle(api, idToken, Platform.OS !== "web");

        if (response.status === "signedIn") {
          return response.session;
        }

        continuation = response;
        if (response.status === "linkRequired") {
          nextRoute = "/link-google";
        } else if (response.status === "registrationRequired") {
          nextRoute = "/register-google";
        }

        return null;
      });

      if (continuation && nextRoute) {
        setContinuation(continuation);
        router.replace(nextRoute);
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
      {!pending && (
        <View>
          <Link href="/forgot-password" style={formStyles.link}>Forgot password?</Link>
          <Link href="/register" style={formStyles.link}>Create an account</Link>
        </View>
      )}
    </>
  );
}
