import { Link } from "expo-router";
import { useRef, useState } from "react";
import { Platform, Text, View } from "react-native";

import { useApi } from "@/shared/api/api-context";
import { useAuth } from "@/shared/session/AuthProvider";
import { FormField, FormSubmit, formStyles } from "@/shared/ui/FormScreen";
import { FormError } from "@/shared/ui/FormError";
import { signInWithGoogle } from "../api/sign-in-with-google";
import { requestGoogleIdToken } from "../services/request-google-id-token";
import { getGoogleSignInError } from "../models/google-sign-in-error";
import { getSignInError } from "../models/sign-in-error";
import { GoogleSignInButton } from "./GoogleSignInButton";

export function SignInForm() {
  const api = useApi();
  const { signIn, signInWith } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const submitting = useRef(false);
  const googleIdToken = useRef<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  async function handleSignIn() {
    if (submitting.current) return;

    if (!email.trim() || !password) {
      setError("Enter your email address and password.");
      return;
    }

    googleIdToken.current = null;
    setNotice(null);
    submitting.current = true;
    setPending(true);
    setError(null);
    try {
      await signIn({ email: email.trim(), password });
    } catch (err) {
      setError(getSignInError(err));
    } finally {
      submitting.current = false;
      setPending(false);
    }
  }

  async function handleGoogleSignIn() {
    if (submitting.current) return;

    googleIdToken.current = null;
    setNotice(null);
    submitting.current = true;
    setPending(true);
    setError(null);
    try {
      const idToken = await requestGoogleIdToken(api);

      if (!idToken) {
        return;
      }

      googleIdToken.current = idToken;

      await signInWith(async () => {
        const response = await signInWithGoogle(api, idToken, Platform.OS !== "web");

        if (response.status === "signedIn") {
          return response.session;
        }

        if (response.status === "linkRequired") {
          setNotice("This Google account is not connected to your CRM account. Sign in with your email and password.");
        } else {
          setNotice("No CRM account is connected to this Google account. Create an account to continue.");
        }

        return null;
      });
    } catch (error) {
      setError(getGoogleSignInError(error));
    } finally {
      googleIdToken.current = null;
      submitting.current = false;
      setPending(false);
    }
  }

  return (
    <>
      <FormField
        label="Email address"
        value={email}
        onChangeText={setEmail}
        autoCapitalize="none"
        autoCorrect={false}
        keyboardType="email-address"
        autoComplete="email"
        editable={!pending}
      />
      <FormField
        label="Password"
        value={password}
        onChangeText={setPassword}
        secureTextEntry
        autoComplete="current-password"
        returnKeyType="go"
        onSubmitEditing={handleSignIn}
        editable={!pending}
      />
      <FormError message={error} />
      <FormSubmit label="Sign in" pending={pending} onPress={handleSignIn} />
      <GoogleSignInButton pending={pending} onPress={handleGoogleSignIn} />
      {notice && (
        <Text accessibilityRole="status" style={formStyles.description}>{notice}</Text>
      )}
      {!pending && (
        <View>
          <Link href="/forgot-password" style={formStyles.link}>Forgot password?</Link>
          <Link href="/register" style={formStyles.link}>Create an account</Link>
        </View>
      )}
    </>
  );
}
