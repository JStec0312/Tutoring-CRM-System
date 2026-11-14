import { usePathname, useRouter, type Href } from 'expo-router';
import type { PropsWithChildren } from 'react';
import { ScrollView, StyleSheet, Text, useWindowDimensions, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { DashboardHeader, DashboardSidebar } from '@/shared/components/ui/dashboard-navigation';
import { WorkspaceButton, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { useAuth } from '@/shared/session/AuthProvider';

const destinations: Record<string, Href> = {
  Home: '/tutor',
  Calendar: '/tutor/calendar',
  Students: '/tutor/students',
  Payments: '/tutor/payments',
  Materials: '/tutor/materials',
  Statistics: '/tutor/statistics',
  Notifications: '/tutor/notifications',
  'Profile & security': '/tutor/profile',
};
const sections = Object.keys(destinations);
const compactSections = ['Home', 'Calendar', 'Students', 'Payments', 'More'];

export function TutorWorkspace({ children }: PropsWithChildren) {
  const { signOut } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const compact = useWindowDimensions().width < 760;
  const current = pathname.startsWith('/tutor/agreements') ? 'Students'
    : pathname.startsWith('/tutor/lessons') || pathname === '/tutor/schedule' ? 'Calendar'
    : sections.find(section => section !== 'Home' &&
      (pathname === destinations[section] || pathname.startsWith(`${destinations[section]}/`))) ?? 'Home';
  const nav = <DashboardSidebar compact={compact} sections={compact ? compactSections : sections}
    current={compact && (pathname === '/tutor/more' || !compactSections.includes(current)) ? 'More' : current}
    onSelect={section => router.navigate(section === 'More' ? '/tutor/more' : destinations[section])} />;

  return <SafeAreaView style={styles.screen}>
    <DashboardHeader email="Tutor account" compact={compact}
      onNotifications={() => router.navigate('/tutor/notifications')}
      onSignOut={() => void signOut().catch(() => console.warn('Could not sign out on the server.'))} />
    <View style={styles.body}>
      {!compact && nav}
      <View style={styles.slot}>{children}</View>
    </View>
    {compact && nav}
  </SafeAreaView>;
}

export function TutorPage({ title, subtitle, children, topAligned = false }: PropsWithChildren<{
  title: string;
  subtitle?: string;
  topAligned?: boolean;
}>) {
  const compact = useWindowDimensions().width < 760;
  return <ScrollView keyboardShouldPersistTaps="handled"
    contentContainerStyle={[styles.content, topAligned && styles.topAligned, compact && styles.compactContent]}>
    <Text accessibilityRole="header" style={[ui.heading, compact && styles.compactHeading]}>{title}</Text>
    {subtitle && <Text style={ui.body}>{subtitle}</Text>}
    {children}
  </ScrollView>;
}

export function QueryFeedback({ loading, error, reload }: { loading: boolean; error?: string; reload: () => void }) {
  if (loading) return <Text accessibilityLiveRegion="polite" style={ui.body}>Loading…</Text>;
  if (!error) return null;
  return <View style={ui.stack}>
    <Text accessibilityRole="alert" style={ui.error}>{error}</Text>
    <WorkspaceButton label="Try again" onPress={reload} />
  </View>;
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: '#fff' },
  body: { flex: 1, flexDirection: 'row' },
  slot: { flex: 1, minWidth: 0 },
  content: { width: '100%', maxWidth: 1180, paddingHorizontal: 40, paddingTop: 56, paddingBottom: 40, gap: 16 },
  topAligned: { paddingTop: 28 },
  compactContent: { paddingHorizontal: 16, paddingTop: 20, paddingBottom: 28 },
  compactHeading: { fontSize: 24, lineHeight: 32 },
});
