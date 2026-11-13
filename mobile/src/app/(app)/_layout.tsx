import { Stack } from "expo-router";

import { useAuth } from "@/shared/session/AuthProvider";
import { getPortalRoute } from "@/shared/session/portal-route";

export default function AppLayout() {
  const { roles } = useAuth();
  const portal = getPortalRoute(roles);

  return (
    <Stack screenOptions={{ headerShown: false }}>
      <Stack.Screen name="index" />
      <Stack.Protected guard={portal === "/tutor"} redirectTo={portal ?? "/"}>
        <Stack.Screen name="(tutor)/tutor" />
      </Stack.Protected>
      <Stack.Protected guard={portal === "/student"} redirectTo={portal ?? "/"}>
        <Stack.Screen name="(student)/student" />
      </Stack.Protected>
      <Stack.Protected guard={false} redirectTo={portal ?? "/"}>
        <Stack.Screen name="(parent)/parent" />
      </Stack.Protected>
    </Stack>
  );
}
