import { Link } from "expo-router";
import { useRef, useState } from "react";
import { StyleSheet, Text, View } from "react-native";

import { useApi } from "@/shared/api/api-context";
import { RegistrationRolePicker } from "@/shared/ui/RegistrationRolePicker";
import { FormError } from "@/shared/ui/FormError";
import { FormField, FormScreen, FormSubmit, formStyles } from "@/shared/ui/FormScreen";
import { registerAccount, type RegistrationRole } from "../api/register-account";
import { getRegistrationError } from "../models/registration-error";

export function RegisterScreen() {
  const api = useApi();
  const [role, setRole] = useState<RegistrationRole | null>(null);
  const [email, setEmail] = useState("");
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [registered, setRegistered] = useState(false);
  const submitting = useRef(false);

  async function handleRegister() {
    if (submitting.current) return;

    if (!role) {
      setError("Wybierz typ konta.");
      return;
    }

    if (!email.trim() || !userName.trim() || !password.trim()) {
      setError("Podaj adres e-mail, nazwę użytkownika i hasło.");
      return;
    }

    submitting.current = true;
    setPending(true);
    setError(null);
    try {
      await registerAccount(api, role, { email, userName, password });
      setPassword("");
      setRegistered(true);
    } catch (err) {
      setError(getRegistrationError(err));
    } finally {
      submitting.current = false;
      setPending(false);
    }
  }

  if (registered) {
    return (
      <FormScreen title="Potwierdź adres e-mail">
        <Text style={formStyles.description}>
          Konto zostało utworzone. Otwórz link wysłany na adres {email.trim()}, a następnie się zaloguj.
        </Text>
        <Link href="/sign-in" dismissTo style={formStyles.link}>Przejdź do logowania</Link>
      </FormScreen>
    );
  }

  return (
    <FormScreen title="Utwórz konto">
      <RegistrationRolePicker
        role={role} onChange={setRole} disabled={pending}
        label="Typ konta" studentLabel="Uczeń" tutorLabel="Korepetytor"
      />
      <FormField
        label="Adres e-mail"
        value={email}
        onChangeText={setEmail}
        autoCapitalize="none"
        autoCorrect={false}
        keyboardType="email-address"
        autoComplete="email"
        maxLength={320}
        editable={!pending}
      />
      <FormField
        label="Nazwa użytkownika"
        value={userName}
        onChangeText={setUserName}
        autoCapitalize="none"
        autoCorrect={false}
        autoComplete="username"
        maxLength={100}
        editable={!pending}
      />
      <View style={styles.roleGroup}>
        <FormField
          label="Hasło"
          value={password}
          onChangeText={setPassword}
          secureTextEntry
          autoComplete="new-password"
          returnKeyType="go"
          onSubmitEditing={handleRegister}
          editable={!pending}
        />
        <Text style={formStyles.description}>
          Od 8 do 128 znaków, w tym mała i wielka litera, cyfra oraz znak specjalny.
        </Text>
      </View>
      <FormError message={error} />
      <FormSubmit label="Utwórz konto" pending={pending} onPress={handleRegister} />
      {!pending && (
        <Link href="/sign-in" dismissTo style={formStyles.link}>Masz konto? Zaloguj się</Link>
      )}
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  roleGroup: { gap: 6 },
});
