import { useRef, useState } from "react";
import { ActivityIndicator, Button, Text, TextInput, View } from "react-native";

import { ApiError } from "@/shared/api/api-error";
import { useAuth } from "@/shared/session/AuthProvider";
import { Link } from "expo-router";

function getSignInError(error: unknown): string {
  if (error instanceof ApiError) {
    switch (error.code) {
      case "Auth.InvalidCredentials":
        return "Nieprawidłowy email lub hasło.";

      case "Auth.InactiveAccount":
        return "Konto jest nieaktywne.";

      case "Network.Unavailable":
        return "Nie można połączyć się z serwerem.";
    }
  }

  return "Nie udało się zalogować. Spróbuj ponownie.";
}

export function SignInScreen() {
  const { signIn } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submitting = useRef(false);

  async function handleSignIn() {
    if (submitting.current) return;

    if (!email.trim() || !password) {
      setError("Podaj email i hasło.");
      return;
    }

    submitting.current = true;
    setLoading(true);
    setError(null);

    try {
      await signIn({
        email: email.trim(),
        password,
      });
    } catch (err) {
      setError(getSignInError(err));
    } finally {
      submitting.current = false;
      setLoading(false);
    }
  }

  return (
    <View
      style={{
        flex: 1,
        justifyContent: "center",
        padding: 24,
        gap: 12,
      }}
    >
      <Text>Logowanie</Text>

      <TextInput
        placeholder="Email"
        value={email}
        onChangeText={setEmail}
        autoCapitalize="none"
        autoCorrect={false}
        keyboardType="email-address"
        autoComplete="email"
        editable={!loading}
        style={{ borderWidth: 1, padding: 10 }}
      />

      <TextInput
        placeholder="Hasło"
        value={password}
        onChangeText={setPassword}
        secureTextEntry
        autoComplete="current-password"
        editable={!loading}
        style={{ borderWidth: 1, padding: 10 }}
      />

      {error && (
        <Text accessibilityRole="alert" style={{ color: "red" }}>
          {error}
        </Text>
      )}

      <Button title="Zaloguj się" onPress={handleSignIn} disabled={loading} />
      <Link href="/register">Utwórz konto</Link>
      <Link href="/forgot-password">Nie pamiętam hasła</Link>

      {loading && <ActivityIndicator />}
    </View>
  );
}
