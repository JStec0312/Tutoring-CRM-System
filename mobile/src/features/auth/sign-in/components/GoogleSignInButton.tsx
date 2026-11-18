import { Image, Pressable, StyleSheet } from "react-native";

type Props = {
  pending: boolean;
  onPress: () => void;
};

export function GoogleSignInButton({ pending, onPress }: Props) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel="Sign in with Google"
      accessibilityState={{ busy: pending, disabled: pending }}
      disabled={pending}
      onPress={onPress}
      style={[styles.button, pending && styles.pending]}
    >
      <Image
        source={{ uri: "/images/google-sign-in-light.png" }}
        alt=""
        accessible={false}
        style={styles.image}
        resizeMode="contain"
      />
    </Pressable>
  );
}

const styles = StyleSheet.create({
  button: {
    alignSelf: "center",
    width: 216,
    height: 48,
  },
  pending: { opacity: 0.6 },
  image: { width: "100%", height: "100%" },
});
