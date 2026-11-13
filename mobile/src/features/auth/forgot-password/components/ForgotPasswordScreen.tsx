import { Link } from "expo-router";
import { useRef, useState } from "react";
import { Text } from "react-native";

import { useApi } from "@/shared/api/api-context";
import { FormField, FormScreen, FormSubmit, formStyles } from "@/shared/ui/FormScreen";
import { requestPasswordReset } from "../api/request-password-reset";
import { getRequestPasswordResetError } from "../models/request-password-reset-error";

export function ForgotPasswordScreen() {
  const api = useApi();
  const [email, setEmail] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [requested, setRequested] = useState(false);
  const submitting = useRef(false);

  async function handleRequest() {
    if (submitting.current) return;

    if (!email.trim()) {
      setError("Podaj adres e-mail.");
      return;
    }

    submitting.current = true;
    setPending(true);
    setError(null);
    try {
      await requestPasswordReset(api, email);
      setRequested(true);
    } catch (err) {
      setError(getRequestPasswordResetError(err));
    } finally {
      submitting.current = false;
      setPending(false);
    }
  }

  return (
    <FormScreen title={requested ? "Sprawdź pocztę" : "Odzyskaj hasło"}>
      {requested ? (
        <Text accessibilityRole="alert" style={formStyles.description}>
          Jeśli z tym adresem jest powiązane aktywne konto, otrzymasz link do ustawienia nowego hasła.
        </Text>
      ) : (
        <>
          <FormField
            label="Adres e-mail"
            value={email}
            onChangeText={setEmail}
            autoCapitalize="none"
            autoCorrect={false}
            keyboardType="email-address"
            autoComplete="email"
            maxLength={320}
            returnKeyType="go"
            onSubmitEditing={handleRequest}
            editable={!pending}
          />
          {error && (
            <Text accessibilityRole="alert" style={formStyles.error}>{error}</Text>
          )}
          <FormSubmit label="Wyślij link" pending={pending} onPress={handleRequest} />
        </>
      )}
      {!pending && (
        <Link href="/sign-in" dismissTo style={formStyles.link}>Wróć do logowania</Link>
      )}
    </FormScreen>
  );
}
