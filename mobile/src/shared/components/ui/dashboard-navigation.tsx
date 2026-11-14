import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

export function DashboardHeader({ email, onSignOut, compact }: { email: string; onSignOut: () => void; compact: boolean }) {
  return (
    <View style={styles.header}>
      <Text style={styles.brand}>TUTORING / workspace</Text>
      {!compact && <Text numberOfLines={1} style={styles.account}>{email}</Text>}
      <Pressable accessibilityRole="button" onPress={onSignOut} style={styles.signOut}><Text style={styles.buttonText}>Sign out</Text></Pressable>
    </View>
  );
}

export function DashboardSidebar({ compact, sections, current }: { compact: boolean; sections: readonly string[]; current: string }) {
  if (compact) return null;
  return (
    <ScrollView style={styles.sidebar} contentContainerStyle={styles.sidebarContent}>
      {sections.map(section => <Text key={section} style={[styles.section, section === current && styles.current]}>{section}</Text>)}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  header: { minHeight: 64, borderBottomWidth: 1, borderColor: '#dedede', paddingHorizontal: 24, flexDirection: 'row', alignItems: 'center', gap: 24 },
  brand: { color: '#222', fontSize: 15, fontWeight: '700', flex: 1 },
  account: { color: '#555', fontSize: 13, maxWidth: 240 },
  signOut: { paddingHorizontal: 12, paddingVertical: 8, borderWidth: 1, borderColor: '#aaa', borderRadius: 5 },
  buttonText: { color: '#333', fontSize: 12 },
  sidebar: { width: 170, flexGrow: 0, backgroundColor: '#dedede' },
  sidebarContent: { paddingVertical: 20, gap: 24 },
  section: { color: '#444', paddingHorizontal: 24, fontSize: 13 },
  current: { fontWeight: '700', color: '#222' },
});
