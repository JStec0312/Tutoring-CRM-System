import { Button } from "react-native";

import { useAuth } from "@/shared/session/AuthProvider";

export function SignOutButton() {
  const { signOut } = useAuth();

  async function handleSignOut() {
    try {
      await signOut();
    } catch {
      console.warn("Nie udało się zakończyć sesji na serwerze.");
    }
  }

  return <Button title="Wyloguj się" onPress={handleSignOut} />;
}
