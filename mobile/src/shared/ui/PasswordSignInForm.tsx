import { useRef, useState } from "react";

import type { LoginCredentials } from "@/shared/session/session-api";
import { getSignInError } from "@/shared/session/sign-in-error";
import { FormError } from "./FormError";
import { FormField, FormSubmit } from "./FormScreen";

type Props = {
  pending: boolean;
  onPendingChange: (pending: boolean) => void;
  onSubmit: (credentials: LoginCredentials) => Promise<void>;
  submitLabel?: string;
  fixedEmail?: string;
};

export function PasswordSignInForm({ pending, onPendingChange, onSubmit, submitLabel = "Sign in", fixedEmail }: Props) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const submitting = useRef(false);

  async function handleSubmit() {
    if (pending || submitting.current) return;

    let loginEmail = email;
    if (fixedEmail) {
      loginEmail = fixedEmail;
      if (!password) {
        setError("Enter your password.");
        return;
      }
    }

    if (!loginEmail.trim() || !password) {
      setError("Enter your email address and password.");
      return;
    }

    submitting.current = true;
    onPendingChange(true);
    setError(null);
    try {
      await onSubmit({ email: loginEmail.trim(), password });
      setPassword("");
    } catch (error) {
      setError(getSignInError(error));
    } finally {
      submitting.current = false;
      onPendingChange(false);
    }
  }

  return (
    <>
      {!fixedEmail && (
        <FormField
          label="Email address"
          value={email}
          onChangeText={setEmail}
          autoCapitalize="none"
          autoCorrect={false}
          keyboardType="email-address"
          autoComplete="username"
          editable={!pending}
        />
      )}
      <FormField
        label="Password"
        value={password}
        onChangeText={setPassword}
        secureTextEntry
        autoComplete="current-password"
        returnKeyType="go"
        onSubmitEditing={handleSubmit}
        editable={!pending}
      />
      <FormError message={error} />
      <FormSubmit label={submitLabel} pending={pending} onPress={handleSubmit} />
    </>
  );
}
