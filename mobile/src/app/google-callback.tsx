import { useEffect } from "react";
import { Text } from "react-native";
import { maybeCompleteAuthSession } from "expo-web-browser";

import { FormScreen } from "@/shared/ui/FormScreen";

export default function GoogleCallbackScreen() {
  useEffect(() => {
    maybeCompleteAuthSession();
  }, []);

  return (
    <FormScreen title="Google sign-in">
      <Text>Returning to sign-in...</Text>
    </FormScreen>
  );
}
