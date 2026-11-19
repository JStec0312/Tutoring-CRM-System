import { Stack } from "expo-router";
import { useMemo, useState, type PropsWithChildren } from "react";
import { Platform } from "react-native";

import {
  AuthProvider,
  useAuth,
  useSessionManager,
} from "@/shared/session/AuthProvider";

import { GoogleContinuationProvider } from "@/features/auth/sign-in/models/GoogleContinuationProvider";

import { resolveApiBaseUrl } from "@/shared/api/api-base-url";
import { createApiClient } from "@/shared/api/api-client";
import { ApiProvider } from "@/shared/api/api-context";

import { refreshTokenStorage } from "@/shared/session/refresh-token-storage";
import { createSessionApi } from "@/shared/session/session-api";
import { withSessionLock } from "@/shared/session/session-lock";
import { createSessionManager } from "@/shared/session/session-manager";
import { sessionSync } from "@/shared/session/session-sync";

let webOrigin: string | undefined;
if (Platform.OS === "web" && typeof window !== "undefined") {
  webOrigin = window.location.origin;
}
const BASE_URL = resolveApiBaseUrl(
  process.env.EXPO_PUBLIC_API_URL ?? "",
  process.env.EXPO_PUBLIC_WEB_API_URL,
  webOrigin,
);

const credentials = Platform.OS === "web" ? "include" : "omit";

// Tworzymy zależności sesji.
function createSession() {
  const authHttp = createApiClient({
    baseUrl: BASE_URL,
    credentials,
  });

  const sessionApi = createSessionApi(
    authHttp,
    Platform.OS === "web" ? "web" : "native",
  );

  return createSessionManager({
    api: sessionApi,
    storage: refreshTokenStorage,
    exclusive: withSessionLock,
    sync: sessionSync,
  });
}

// Łączymy SessionManager z klientem HTTP.
function ApiBridge({ children }: PropsWithChildren) {
  const manager = useSessionManager();

  const api = useMemo(
    () =>
      createApiClient({
        baseUrl: BASE_URL,
        credentials,
        session: manager,
      }),
    [manager],
  );

  return <ApiProvider client={api}>{children}</ApiProvider>;
}

// Decydujemy, które grupy tras są dostępne.
function AppNavigator() {
  const { status } = useAuth();
  if (status === "restoring") return null;
  const restoring = status === "restore-error";

  return (
    <Stack screenOptions={{ headerShown: false }}>
      <Stack.Protected
        guard={status === "authenticated"}
        redirectTo={restoring ? "/session-status" : "/sign-in"}
      >
        <Stack.Screen name="(app)" />
      </Stack.Protected>

      <Stack.Protected
        guard={status === "anonymous"}
        redirectTo={restoring ? "/session-status" : "/"}
      >
        <Stack.Screen name="(auth)/sign-in" />
        <Stack.Screen name="(auth)/register" />
        <Stack.Screen name="(auth)/register-google" />
      </Stack.Protected>

      <Stack.Protected
        guard={restoring}
        redirectTo={status === "authenticated" ? "/" : "/sign-in"}
      >
        <Stack.Screen name="session-status" />
      </Stack.Protected>

      <Stack.Screen name="(auth)/link-google" />
      <Stack.Screen name="(auth)/forgot-password" />
      <Stack.Screen name="(auth)/reset-password" />
    </Stack>
  );
}

export default function RootLayout() {
  const [manager] = useState(createSession);

  return (
    <AuthProvider manager={manager}>
      <ApiBridge>
        <GoogleContinuationProvider>
          <AppNavigator />
        </GoogleContinuationProvider>
      </ApiBridge>
    </AuthProvider>
  );
}
