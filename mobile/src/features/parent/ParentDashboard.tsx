import { Text } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";

import { SignOutButton } from "@/shared/components/ui/SignOutButton";

export function ParentDashboard() {
  return (
    <SafeAreaView style={{ flex: 1, alignItems: "center", justifyContent: "center", padding: 24 }}>
      <Text accessibilityRole="header">Parent Portal</Text>
      <SignOutButton />
    </SafeAreaView>
  );
}
