import { Pressable, StyleSheet, Text, View } from "react-native";

import type { RegistrationRole } from "@/shared/session/registration-role";

type Props = {
  role: RegistrationRole | null;
  onChange: (role: RegistrationRole) => void;
  disabled: boolean;
  label?: string;
  studentLabel?: string;
  tutorLabel?: string;
};

export function RegistrationRolePicker({
  role, onChange, disabled, label = "Account type", studentLabel = "Student", tutorLabel = "Tutor",
}: Props) {
  const choices = [
    { value: "student", label: studentLabel },
    { value: "tutor", label: tutorLabel },
  ] as const;

  return (
    <View style={styles.group}>
      <Text style={styles.label}>{label}</Text>
      <View style={styles.choices} accessibilityRole="radiogroup" accessibilityLabel={label}>
        {choices.map((choice) => (
          <Pressable
            key={choice.value}
            accessibilityRole="radio"
            accessibilityState={{ checked: role === choice.value, disabled }}
            disabled={disabled}
            onPress={() => onChange(choice.value)}
            style={[styles.choice, role === choice.value && styles.selected]}
          >
            <Text style={styles.label}>{choice.label}</Text>
          </Pressable>
        ))}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  group: { gap: 6 },
  choices: { flexDirection: "row", flexWrap: "wrap", gap: 8 },
  choice: {
    minHeight: 48, justifyContent: "center", paddingHorizontal: 12, paddingVertical: 8,
    borderWidth: 1, borderColor: "#94a3b8", borderRadius: 8,
  },
  selected: { borderColor: "#1d4ed8", backgroundColor: "#eff6ff" },
  label: { color: "#0f172a", fontSize: 15, fontWeight: "600" },
});
