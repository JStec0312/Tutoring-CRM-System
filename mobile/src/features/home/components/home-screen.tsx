import { Button, ScrollView, StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useSession } from '@/shared/session/session-context';
import { Colors } from '@/shared/theme/theme';
import { HomeCopy } from '../models/home-copy';

export default function HomeScreen() {
  const { user, signOut } = useSession();
  return (
    <SafeAreaView style={styles.container}>
      <ScrollView contentContainerStyle={styles.content}>
        <Text style={styles.title}>{HomeCopy.Title}</Text>
        <Text style={styles.text}>{user?.email}</Text>
        <Text style={styles.text}>{user?.roles.join(HomeCopy.RoleSeparator)}</Text>
        <View style={styles.modules}>
          <Text style={styles.text}>{HomeCopy.Modules}</Text>
          {HomeCopy.FeatureNames.map(name => <Text key={name} style={styles.text}>{name}</Text>)}
        </View>
        <Button title={HomeCopy.SignOut} onPress={() => void signOut().catch(() => undefined)} />
      </ScrollView>
    </SafeAreaView>
  );
}
const styles = StyleSheet.create({
  container: { flex: 1 },
  content: { padding: 24, paddingBottom: 120, gap: 16, maxWidth: 800, width: '100%', alignSelf: 'center' },
  title: { fontSize: 28, fontWeight: '600', color: Colors.light.text },
  text: { color: Colors.light.text },
  modules: { gap: 8 },
});
