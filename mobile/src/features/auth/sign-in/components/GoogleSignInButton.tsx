import { Pressable, StyleSheet, Text } from "react-native";

type Props = {
  pending: boolean;
  onPress: () => void;
};

export function GoogleSignInButton({ pending, onPress }: Props) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ busy: pending, disabled: pending }}
      disabled={pending}
      onPress={onPress}
      style={[styles.googleButton, pending && styles.pending]}
    >
      <Text style={styles.googleButtonText}>Sign in with Google</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  googleButton: {
    minHeight: 48,
    alignItems: "center",
    justifyContent: "center",
    borderColor: "#94a3b8",
    borderWidth: 1,
    borderRadius: 8,
    padding: 12,
  },
  pending: { opacity: 0.6 },
  googleButtonText: {
    color: "#0f172a",
    fontSize: 16,
    fontWeight: "600",
  },
});
