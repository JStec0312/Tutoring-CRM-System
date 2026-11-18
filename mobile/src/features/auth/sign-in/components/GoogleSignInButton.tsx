import { Pressable, StyleSheet, Text } from "react-native";

export function GoogleSignInButton() {
  return (
    <Pressable accessibilityRole="button" style={styles.googleButton}>
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
  googleButtonText: {
    color: "#0f172a",
    fontSize: 16,
    fontWeight: "600",
  },
});
