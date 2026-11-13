import { Link } from "expo-router";
import { useRef, useState } from "react";
import { Text } from "react-native";

import { useApi } from "@/shared/api/api-context";
import { FormField, FormScreen, FormSubmit, formStyles } from "@/shared/ui/FormScreen";
import { resetPassword } from "../api/reset-password";
import { getResetPasswordError } from "../models/reset-password-error";

export function ResetPasswordScreen({ token }: { token: string }) {
  const api = useApi();
  const [password, setPassword] = useState("");
  const [confirmation, setConfirmation] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<ReturnType<typeof getResetPasswordError> | null>(null);
  const [completed, setCompleted] = useState(false);
  const submitting = useRef(false);

  async function handleReset() {
    if (submitting.current) return;

    if (!password.trim() || password !== confirmation) {
      setError({
        message: password.trim() ? "Hasła muszą być takie same." : "Podaj nowe hasło.",
      });
      return;
    }

    submitting.current = true;
    setPending(true);
    setError(null);
    try {
      await resetPassword(api, token, password);
      setPassword("");
      setConfirmation("");
      setCompleted(true);
    } catch (err) {
      setError(getResetPasswordError(err));
    } finally {
      submitting.current = false;
      setPending(false);
    }
  }

  if (!token || error?.invalidLink) {
    return (
      <FormScreen title="Link jest niedostępny">
        <Text accessibilityRole="alert" style={formStyles.error}>
          {error?.message ?? "Otwórz pełny link z wiadomości e-mail."}
        </Text>
        <Link href="/forgot-password" replace style={formStyles.link}>Poproś o nowy link</Link>
      </FormScreen>
    );
  }

  if (completed) {
    return (
      <FormScreen title="Hasło zostało zmienione">
        <Link href="/sign-in" replace style={formStyles.link}>Przejdź do logowania</Link>
      </FormScreen>
    );
  }

  return (
    <FormScreen title="Ustaw nowe hasło">
      <FormField
        label="Nowe hasło"
        value={password}
        onChangeText={setPassword}
        secureTextEntry
        autoComplete="new-password"
        editable={!pending}
      />
      <Text style={formStyles.description}>
        Od 8 do 128 znaków, w tym mała i wielka litera, cyfra oraz znak specjalny.
      </Text>
      <FormField
        label="Powtórz nowe hasło"
        value={confirmation}
        onChangeText={setConfirmation}
        secureTextEntry
        autoComplete="new-password"
        returnKeyType="go"
        onSubmitEditing={handleReset}
        editable={!pending}
      />
      {error && (
        <Text accessibilityRole="alert" style={formStyles.error}>{error.message}</Text>
      )}
      <FormSubmit label="Zapisz nowe hasło" pending={pending} onPress={handleReset} />
    </FormScreen>
  );
}
