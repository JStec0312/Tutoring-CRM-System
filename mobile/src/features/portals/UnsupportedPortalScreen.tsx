import { Button, Text } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";

import { useAuth } from "@/shared/session/AuthProvider";

export function UnsupportedPortalScreen() {
  const { signOut } = useAuth();

  async function handleSignOut() {
    try {
      await signOut();
    } catch {
      console.warn("Nie udało się zakończyć sesji na serwerze.");
    }
  }

  return (
    <SafeAreaView style={{ flex: 1, justifyContent: "center", padding: 24, gap: 12 }}>
      <Text accessibilityRole="alert">Nie można wybrać portalu dla tego konta.</Text>
      <Button title="Wyloguj się" onPress={handleSignOut} />
    </SafeAreaView>
  );
}
