import { useEffect, useState } from 'react';
import { Button, ScrollView, StyleSheet, Text, useWindowDimensions, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { DashboardHeader, DashboardSidebar } from '@/shared/components/ui/dashboard-navigation';
import { AttentionNotice, LessonList, SummaryCards } from './overview';
import { useAuth } from '@/shared/session/AuthProvider';
import { useApi } from '@/shared/api/api-context';
import { getTutorDashboard, type DashboardData } from './dashboard-api';

const sections = ['Overview', 'Calendar', 'Students', 'Settlements', 'Materials', 'Statistics', 'Notifications', 'Profile'];

export default function TutorDashboard() {
  const { signOut } = useAuth();
  const api = useApi();
  const [data, setData] = useState<DashboardData | null>(null);
  const [error, setError] = useState(false);
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    const controller = new AbortController();
    setError(false);
    setData(null);
    void getTutorDashboard(api, new Date(), controller.signal).then(setData).catch(() => {
      if (!controller.signal.aborted) setError(true);
    });
    return () => controller.abort();
  }, [api, attempt]);
  const onSignOut = () => void signOut().catch(() => console.warn('Could not sign out on the server.'));
  const compact = useWindowDimensions().width < 700;
  const today = new Intl.DateTimeFormat('en-GB', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }).format(new Date());

  return (
    <SafeAreaView style={styles.screen}>
      <DashboardHeader email="Tutor account" onSignOut={onSignOut} compact={compact} />
      <View style={styles.body}>
        <DashboardSidebar compact={compact} sections={sections} current="Overview" />
        <ScrollView style={styles.scroll} contentContainerStyle={styles.content}>
          <Text style={styles.breadcrumb}>TUTOR  /  OVERVIEW</Text>
          <Text style={styles.title}>Your teaching day</Text>
          <Text style={styles.date}>{today}</Text>
          {error ? <View><Text accessibilityRole="alert">Could not load your dashboard.</Text><Button title="Try again" onPress={() => setAttempt(value => value + 1)} /></View> : !data ? <Text>Loading dashboard…</Text> : <>
            <SummaryCards data={data} />
            <LessonList compact={compact} lessons={data.lessons} />
            <AttentionNotice count={data.overdueAgreements} />
          </>}
        </ScrollView>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: '#fff' },
  body: { flex: 1, flexDirection: 'row' },
  scroll: { flex: 1 },
  content: { width: '100%', maxWidth: 1000, paddingHorizontal: 28, paddingTop: 28, paddingBottom: 48, gap: 16 },
  breadcrumb: { color: '#888', fontSize: 10, letterSpacing: 0.6 },
  title: { color: '#252525', fontSize: 28, fontWeight: '700' },
  date: { color: '#555', fontSize: 13 },
});
