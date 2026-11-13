import { ActivityIndicator, Button, Text, View } from "react-native";

import { useAuth, useSessionManager } from "@/shared/session/AuthProvider";

export default function SessionStatusScreen() {
  const { status } = useAuth();
  const manager = useSessionManager();

  if (status === "restoring") {
    return (
      <View style={container}>
        <ActivityIndicator size="large" />
        <Text>Przywracanie sesji...</Text>
      </View>
    );
  }

  return (
    <View style={container}>
      <Text>Nie udało się przywrócić sesji.</Text>

      <Button
        title="Spróbuj ponownie"
        onPress={() => void manager.restoreSession()}
      />
    </View>
  );
}

const container = {
  flex: 1,
  justifyContent: "center" as const,
  alignItems: "center" as const,
  gap: 16,
  padding: 24,
};
