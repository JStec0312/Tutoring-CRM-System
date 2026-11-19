import { Link, usePathname, useRouter } from 'expo-router';
import type { PropsWithChildren } from 'react';
import { ScrollView, StyleSheet, Text, useWindowDimensions, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { DashboardHeader } from '@/shared/components/ui/dashboard-navigation';
import { WorkspaceButton, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { useAuth } from '@/shared/session/AuthProvider';

const destinations = [
  { label: 'Overview', href: '/student' },
  { label: 'Calendar', href: '/student/calendar' },
  { label: 'My lessons', href: '/student/lessons' },
  { label: 'My tutors', href: '/student/tutors' },
  { label: 'Settlements', href: '/student/payments' },
  { label: 'Notifications', href: '/student/notifications' },
  { label: 'Profile', href: '/student/profile' },
] as const;

export function StudentWorkspace({ children }: PropsWithChildren) {
  const { signOut } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const compact = useWindowDimensions().width < 760;
  let current = '/student';
  for (const destination of destinations.slice(1)) {
    if (pathname.startsWith(destination.href)) current = destination.href;
  }
  if (pathname.startsWith('/student/materials')) current = '/student/lessons';

  const navigation = <ScrollView horizontal={compact} role="navigation" accessibilityLabel="Student workspace"
    style={[styles.navigation, compact && styles.bottomNavigation]}
    contentContainerStyle={[styles.links, compact && styles.bottomLinks]}>
    {destinations.map(destination => {
      let currentPage: 'page' | undefined;
      if (current === destination.href) currentPage = 'page';
      return <Link key={destination.href} href={destination.href} aria-current={currentPage}
        style={[styles.link, current === destination.href && styles.active]}>{destination.label}</Link>;
    })}
  </ScrollView>;

  return <SafeAreaView style={styles.screen}>
    <DashboardHeader email="Student account" compact={compact}
      onNotifications={() => router.navigate('/student/notifications')}
      onSignOut={() => void signOut().catch(() => console.warn('Could not sign out on the server.'))} />
    <View style={styles.body}>
      {!compact && navigation}
      <View style={styles.slot}>{children}</View>
    </View>
    {compact && navigation}
  </SafeAreaView>;
}

export function StudentPage({ title, children, code }: PropsWithChildren<{ title: string; code?: string }>) {
  const compact = useWindowDimensions().width < 760;
  return <ScrollView role="main" keyboardShouldPersistTaps="handled"
    contentContainerStyle={[styles.content, compact && styles.compactContent]}>
    {code && <Text style={styles.breadcrumb}>{code}   /   STUDENT</Text>}
    <Text role="heading" aria-level={1} style={styles.heading}>{title}</Text>
    {children}
  </ScrollView>;
}

export function StudentQueryFeedback({ loading, error, reload }: { loading: boolean; error?: string; reload: () => void }) {
  if (loading) return <Text role="progressbar" accessibilityLabel="Loading data" style={ui.body}>Loading…</Text>;
  if (!error) return null;
  return <View style={ui.stack}>
    <Text role="alert" style={ui.error}>{error}</Text>
    <WorkspaceButton label="Try again" onPress={reload} />
  </View>;
}

export const studentStyles = StyleSheet.create({
  notice: { backgroundColor: '#d9d9d9', paddingHorizontal: 16, paddingVertical: 12 },
  noticeText: { color: '#555', fontSize: 13, lineHeight: 19 },
});

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: '#fff' },
  body: { flex: 1, flexDirection: 'row' },
  slot: { flex: 1, minWidth: 0 },
  navigation: { width: 220, flexGrow: 0, backgroundColor: '#d9d9d9' },
  links: { paddingVertical: 28, paddingHorizontal: 20, gap: 8 },
  bottomNavigation: { width: '100%', height: 56, maxHeight: 56, borderTopWidth: 1, borderTopColor: '#ccc' },
  bottomLinks: { padding: 4, flexDirection: 'row', alignItems: 'center', gap: 4 },
  link: { color: '#292929', fontSize: 15, lineHeight: 22, paddingHorizontal: 12, paddingVertical: 11, minHeight: 44 },
  active: { backgroundColor: '#dbdbdb', fontWeight: '600' },
  content: { width: '100%', maxWidth: 950, paddingTop: 28, paddingHorizontal: 40, paddingBottom: 40, gap: 20 },
  compactContent: { paddingHorizontal: 16, paddingTop: 20 },
  breadcrumb: { fontSize: 11, color: '#6e6e6e', lineHeight: 16 },
  heading: { color: '#292929', fontSize: 30, lineHeight: 44, fontWeight: '600' },
});
