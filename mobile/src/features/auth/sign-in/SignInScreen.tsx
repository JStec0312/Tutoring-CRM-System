import { Link } from "expo-router";
import { useRef, useState } from "react";
import { Text, View } from "react-native";

import { useAuth } from "@/shared/session/AuthProvider";
import { FormField, FormScreen, FormSubmit, formStyles } from "@/shared/ui/FormScreen";
import { getSignInError } from "./sign-in-error";

export function SignInScreen() {
  const { signIn } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const submitting = useRef(false);

  async function handleSignIn() {
    if (submitting.current) return;

    if (!email.trim() || !password) {
      setError("Podaj adres e-mail i hasło.");
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
    <FormScreen title="Zaloguj się">
      <FormField
        label="Adres e-mail"
        value={email}
        onChangeText={setEmail}
        autoCapitalize="none"
        autoCorrect={false}
        keyboardType="email-address"
        autoComplete="email"
        editable={!pending}
      />
      <FormField
        label="Hasło"
        value={password}
        onChangeText={setPassword}
        secureTextEntry
        autoComplete="current-password"
        returnKeyType="go"
        onSubmitEditing={handleSignIn}
        editable={!pending}
      />
      {error && (
        <Text accessibilityRole="alert" style={formStyles.error}>{error}</Text>
      )}
      <FormSubmit label="Zaloguj się" pending={pending} onPress={handleSignIn} />
      {!pending && (
        <View>
          <Link href="/forgot-password" style={formStyles.link}>Nie pamiętam hasła</Link>
          <Link href="/register" style={formStyles.link}>Utwórz konto</Link>
        </View>
      )}
    </FormScreen>
  );
}
