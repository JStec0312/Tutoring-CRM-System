import { Link } from "expo-router";
import { useRef, useState } from "react";
import { Pressable, StyleSheet, Text, View } from "react-native";

import { useApi } from "@/shared/api/api-context";
import { FormField, FormScreen, FormSubmit, formStyles } from "@/shared/ui/FormScreen";
import { registerAccount, type RegistrationRole } from "../api/register-account";
import { getRegistrationError } from "../models/registration-error";

const roles = [
  { value: "student", label: "Uczeń" },
  { value: "tutor", label: "Korepetytor" },
] as const;

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
      <View style={styles.roleGroup}>
        <Text style={styles.label}>Typ konta</Text>
        <View style={styles.roles} accessibilityRole="radiogroup" accessibilityLabel="Typ konta">
          {roles.map(({ value, label }) => (
            <Pressable
              key={value}
              accessibilityRole="radio"
              accessibilityState={{ checked: role === value, disabled: pending }}
              disabled={pending}
              onPress={() => setRole(value)}
              style={[styles.role, role === value && styles.selectedRole]}
            >
              <Text style={styles.label}>{label}</Text>
            </Pressable>
          ))}
        </View>
      </View>
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
      {error && (
        <Text accessibilityRole="alert" style={formStyles.error}>{error}</Text>
      )}
      <FormSubmit label="Utwórz konto" pending={pending} onPress={handleRegister} />
      {!pending && (
        <Link href="/sign-in" dismissTo style={formStyles.link}>Masz konto? Zaloguj się</Link>
      )}
    </FormScreen>
  );
}

const styles = StyleSheet.create({
  roleGroup: { gap: 6 },
  roles: { flexDirection: "row", flexWrap: "wrap", gap: 8 },
  role: {
    minHeight: 48,
    justifyContent: "center",
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderWidth: 1,
    borderColor: "#94a3b8",
    borderRadius: 8,
  },
  selectedRole: { borderColor: "#1d4ed8", backgroundColor: "#eff6ff" },
  label: { color: "#0f172a", fontSize: 15, fontWeight: "600" },
});
