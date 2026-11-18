import { Link } from "expo-router";
import { useRef, useState } from "react";
import { View } from "react-native";

import { useAuth } from "@/shared/session/AuthProvider";
import { FormField, FormSubmit, formStyles } from "@/shared/ui/FormScreen";
import { FormError } from "@/shared/ui/FormError";
import { getSignInError } from "../models/sign-in-error";
import { GoogleSignInButton } from "./GoogleSignInButton";

export function SignInForm() {
  const { signIn } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const submitting = useRef(false);

  async function handleSignIn() {
    if (submitting.current) return;

    if (!email.trim() || !password) {
      setError("Enter your email address and password.");
      return;
    }

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
      <GoogleSignInButton />
      {!pending && (
        <View>
          <Link href="/forgot-password" style={formStyles.link}>Forgot password?</Link>
          <Link href="/register" style={formStyles.link}>Create an account</Link>
        </View>
      )}
    </>
  );
}
