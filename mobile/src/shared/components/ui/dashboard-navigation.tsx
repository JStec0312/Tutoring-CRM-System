import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { WorkspaceButton } from './workspace-controls';

export function DashboardHeader({ email, onSignOut, compact, onNotifications }: {
  email: string; onSignOut: () => void; compact: boolean; onNotifications?: () => void;
}) {
  return (
    <View style={[styles.header, compact && styles.compactHeader]}>
      <Text style={styles.brand}>TUTORING / workspace</Text>
      {!compact && <Text numberOfLines={1} style={styles.account}>{email}</Text>}
      {!compact && onNotifications && <WorkspaceButton label="Notifications" onPress={onNotifications} />}
      <Pressable accessibilityRole="button" onPress={onSignOut} style={styles.signOut}>
        <Text style={styles.buttonText}>Sign out</Text>
      </Pressable>
    </View>
  );
}

export function DashboardSidebar({ compact, sections, current, onSelect }: {
  compact: boolean;
  sections: readonly string[];
  current: string;
  onSelect?: (section: string) => void;
}) {
  if (compact && !onSelect) return null;
  return (
    <ScrollView horizontal={compact} style={compact ? styles.bottomNav : styles.sidebar}
      contentContainerStyle={compact ? styles.bottomContent : styles.sidebarContent}>
      {sections.map(section => {
        const selected = section === current;
        const label = <Text style={[styles.section, selected && styles.current]}>{section}</Text>;
        return onSelect
          ? <Pressable key={section} accessibilityRole="link" accessibilityState={{ selected }}
              onPress={() => onSelect(section)} style={[styles.navItem, selected && styles.active]}>{label}</Pressable>
          : <View key={section} style={styles.navItem}>{label}</View>;
      })}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  header: { minHeight: 82, borderWidth: 1, borderColor: '#d1d1d1', paddingHorizontal: 28, flexDirection: 'row', alignItems: 'center', gap: 24 },
  compactHeader: { minHeight: 58, paddingHorizontal: 16, gap: 12 },
  brand: { color: '#292929', fontSize: 19, fontWeight: '600', flex: 1 },
  account: { color: '#292929', fontSize: 14, flex: 0.47, maxWidth: 240 },
  signOut: { paddingHorizontal: 12, paddingVertical: 8, borderWidth: 1, borderColor: '#aaa', borderRadius: 5 },
  buttonText: { color: '#333', fontSize: 12 },
  sidebar: { width: 220, flexGrow: 0, backgroundColor: '#d9d9d9' },
  sidebarContent: { paddingVertical: 28, paddingHorizontal: 20, gap: 8 },
  bottomNav: { flexGrow: 0, backgroundColor: '#d9d9d9', borderTopColor: '#ccc', borderTopWidth: 1 },
  bottomContent: { padding: 4, gap: 4 },
  navItem: { minHeight: 42, justifyContent: 'center', paddingHorizontal: 12 },
  active: { backgroundColor: '#e0e0e0' },
  section: { color: '#292929', fontSize: 15 },
  current: { fontWeight: '600' },
});
