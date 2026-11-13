import { Text } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";

import { SignOutButton } from "./SignOutButton";

export function TutorPortalScreen() {
  return (
    <SafeAreaView style={{ flex: 1, alignItems: "center", justifyContent: "center", padding: 24 }}>
      <Text accessibilityRole="header">Tutor Portal</Text>
      <SignOutButton />
    </SafeAreaView>
  );
}
