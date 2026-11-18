import { Pressable, StyleSheet, Text } from "react-native";

import { formStyles } from "./FormScreen";

type Props = {
  label: string;
  onPress: () => void;
  disabled?: boolean;
};

export function FormTextButton({ label, onPress, disabled = false }: Props) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ disabled }}
      disabled={disabled}
      onPress={onPress}
      style={[styles.button, disabled && styles.disabled]}
    >
      <Text style={formStyles.link}>{label}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  button: { minHeight: 48, justifyContent: "center" },
  disabled: { opacity: 0.5 },
});
