import { useState, type PropsWithChildren } from "react";
import {
  ActivityIndicator,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
  type TextInputProps,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";

export function FormScreen({
  title,
  children,
}: PropsWithChildren<{ title: string }>) {
  return (
    <SafeAreaView style={styles.screen}>
      <KeyboardAvoidingView
        style={styles.keyboardArea}
        behavior={Platform.OS === "ios" ? "padding" : undefined}
      >
        <ScrollView
          contentContainerStyle={styles.content}
          keyboardShouldPersistTaps="handled"
        >
          <View style={styles.form}>
            <Text accessibilityRole="header" style={styles.title}>{title}</Text>
            {children}
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

export function FormField({ label, ...input }: TextInputProps & { label: string }) {
  const [focused, setFocused] = useState(false);

  return (
    <View style={styles.field}>
      <Text style={styles.label}>{label}</Text>
      <TextInput
        {...input}
        accessibilityLabel={label}
        onFocus={(event) => {
          setFocused(true);
          input.onFocus?.(event);
        }}
        onBlur={(event) => {
          setFocused(false);
          input.onBlur?.(event);
        }}
        style={[styles.input, focused && styles.focused, input.style]}
      />
    </View>
  );
}

export function FormSubmit({
  label,
  pending,
  onPress,
}: {
  label: string;
  pending: boolean;
  onPress: () => void;
}) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={label}
      accessibilityState={{ busy: pending, disabled: pending }}
      disabled={pending}
      onPress={onPress}
      style={({ pressed }) => [
        styles.submit,
        (pressed || pending) && styles.submitMuted,
      ]}
    >
      {pending && <ActivityIndicator color="#fff" />}
      <Text style={styles.submitText}>{label}</Text>
    </Pressable>
  );
}

export const formStyles = StyleSheet.create({
  description: { color: "#475569", fontSize: 15, lineHeight: 22 },
  error: { color: "#991b1b", fontSize: 15, lineHeight: 22 },
  link: {
    minHeight: 44,
    color: "#1d4ed8",
    fontSize: 15,
    fontWeight: "600",
    paddingVertical: 12,
  },
});

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: "#fff" },
  keyboardArea: { flex: 1 },
  content: { flexGrow: 1, justifyContent: "center", padding: 20 },
  form: { alignSelf: "center", width: "100%", maxWidth: 420, gap: 16 },
  title: { color: "#0f172a", fontSize: 28, fontWeight: "700" },
  field: { gap: 6 },
  label: { color: "#0f172a", fontSize: 15, fontWeight: "600" },
  input: {
    borderColor: "#94a3b8",
    borderWidth: 1,
    borderRadius: 8,
    color: "#0f172a",
    fontSize: 16,
    minHeight: 48,
    paddingHorizontal: 12,
    paddingVertical: 10,
  },
  focused: { borderColor: "#1d4ed8" },
  submit: {
    minHeight: 48,
    alignItems: "center",
    justifyContent: "center",
    flexDirection: "row",
    gap: 8,
    borderRadius: 8,
    padding: 12,
    backgroundColor: "#1d4ed8",
  },
  submitMuted: { backgroundColor: "#1e40af" },
  submitText: { color: "#fff", fontSize: 16, fontWeight: "600" },
});
