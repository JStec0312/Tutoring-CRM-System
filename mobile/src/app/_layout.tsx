import { DefaultTheme, Stack, ThemeProvider } from 'expo-router';
import * as SplashScreen from 'expo-splash-screen';
import { StatusBar } from 'expo-status-bar';
import { useEffect } from 'react';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { api, session } from '@/bootstrap/runtime';
import { SessionGate } from '@/features/auth';
import { ApiProvider } from '@/shared/api/api-context';
import { RouteName } from '@/shared/config/navigation';
import { SessionProvider, useSession } from '@/shared/session/session-context';
import { SessionStatus } from '@/shared/session/session-types';
import { Colors } from '@/shared/theme/theme';

void SplashScreen.preventAutoHideAsync().catch(() => undefined);

const navigationTheme = {
  ...DefaultTheme,
  colors: {
    ...DefaultTheme.colors,
    background: Colors.light.background,
    text: Colors.light.text,
  },
};

function SessionRoutes() {
  const { status, isLoggedIn } = useSession();
  const waiting = status === SessionStatus.Restoring || status === SessionStatus.RestoreError;
  useEffect(() => {
    if (status !== SessionStatus.Restoring) void SplashScreen.hideAsync();
  }, [status]);
  if (waiting) return <SessionGate />;
  return (
    <Stack screenOptions={{ headerShown: false }}>
      <Stack.Protected guard={!isLoggedIn}><Stack.Screen name={RouteName.SignIn} /></Stack.Protected>
      <Stack.Protected guard={isLoggedIn}><Stack.Screen name={RouteName.Workspace} /></Stack.Protected>
    </Stack>
  );
}

export default function RootLayout() {
  return (
    <SafeAreaProvider style={{ backgroundColor: Colors.light.background }}>
      <StatusBar style="dark" />
      <ThemeProvider value={navigationTheme}>
        <SessionProvider session={session}>
          <ApiProvider client={api}><SessionRoutes /></ApiProvider>
        </SessionProvider>
      </ThemeProvider>
    </SafeAreaProvider>
  );
}
