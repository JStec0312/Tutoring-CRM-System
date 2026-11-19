import { usePathname, useRouter, type Href } from 'expo-router';
import type { PropsWithChildren } from 'react';
import { ScrollView, StyleSheet, Text, useWindowDimensions, View, type StyleProp, type TextStyle, type ViewStyle } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { DashboardHeader, DashboardSidebar } from '@/shared/components/ui/dashboard-navigation';
import { WorkspaceButton, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { useAuth } from '@/shared/session/AuthProvider';

const destinations: Record<string, Href> = {
  Home: '/tutor',
  Calendar: '/tutor/calendar',
  'My lessons': '/tutor/lessons',
  Students: '/tutor/students',
  Payments: '/tutor/payments',
  Statistics: '/tutor/statistics',
  Notifications: '/tutor/notifications',
  'Profile & security': '/tutor/profile',
};
const sections = Object.keys(destinations);
const compactSections = ['Home', 'Calendar', 'My lessons', 'Students', 'More'];

export function TutorWorkspace({ children }: PropsWithChildren) {
  const { signOut } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const compact = useWindowDimensions().width < 760;
  let current = 'Home';
  for (const section of sections.slice(1)) {
    const destination = destinations[section];
    if (pathname === destination || pathname.startsWith(`${destination}/`)) current = section;
  }
  if (pathname.startsWith('/tutor/agreements')) current = 'Students';
  if (pathname.startsWith('/tutor/materials')) current = 'My lessons';
  if (pathname === '/tutor/schedule') current = 'Calendar';
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

export function TutorPage({ title, subtitle, children, topAligned = false, eyebrow, contentContainerStyle, headingStyle }: PropsWithChildren<{
  title: string;
  subtitle?: string;
  topAligned?: boolean;
  eyebrow?: string;
  contentContainerStyle?: StyleProp<ViewStyle>;
  headingStyle?: StyleProp<TextStyle>;
}>) {
  const compact = useWindowDimensions().width < 760;
  return <ScrollView keyboardShouldPersistTaps="handled"
    contentContainerStyle={[styles.content, topAligned && styles.topAligned, contentContainerStyle, compact && styles.compactContent]}>
    {eyebrow && <Text style={styles.eyebrow}>{eyebrow}</Text>}
    <Text accessibilityRole="header" style={[ui.heading, headingStyle, compact && styles.compactHeading]}>{title}</Text>
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
  eyebrow: { color: '#666', fontSize: 11, lineHeight: 16 },
  compactHeading: { fontSize: 24, lineHeight: 32 },
});
