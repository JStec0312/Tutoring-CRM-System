import { Button, Text, View } from "react-native";

import { useAuth } from "@/shared/session/AuthProvider";

export default function HomeScreen() {
  const { signOut } = useAuth();

  async function handleSignOut() {
    try {
      await signOut();
    } catch {
      console.warn("Nie udało się zakończyć sesji na serwerze.");
    }
  }

  return (
    <View style={{ flex: 1, justifyContent: "center", padding: 24, gap: 12 }}>
      <Text>Jesteś zalogowany.</Text>
      <Button title="Wyloguj się" onPress={handleSignOut} />
    </View>
  );
}
