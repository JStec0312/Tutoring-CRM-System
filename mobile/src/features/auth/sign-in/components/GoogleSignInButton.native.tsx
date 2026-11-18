import { StyleSheet } from "react-native";
import { GoogleSignInButton as NativeGoogleSignInButton } from "react-native-nitro-google-signin";

type Props = {
  pending: boolean;
  onPress: () => void;
};

export function GoogleSignInButton({ pending, onPress }: Props) {
  return (
    <NativeGoogleSignInButton
      colorScheme="light"
      size="standard"
      signInBehavior="none"
      onPress={onPress}
      disabled={pending}
      loading={pending}
      accessibilityLabel="Sign in with Google"
      accessibilityState={{ busy: pending, disabled: pending }}
      style={styles.button}
    />
  );
}

const styles = StyleSheet.create({
  button: { alignSelf: "center" },
});
