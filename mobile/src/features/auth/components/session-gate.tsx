import { ActivityIndicator, Button, StyleSheet, Text, View } from 'react-native';
import { useSession } from '@/shared/session/session-context';
import { SessionStatus } from '@/shared/session/session-types';
import { Colors } from '@/shared/theme/theme';
import { AuthCopy } from '../models/auth-copy';

export function SessionGate() {
  const { status, error, restoreSession } = useSession();
  const failed = status === SessionStatus.RestoreError;
  return (
    <View style={styles.container}>
      {failed ? <>
        <Text style={styles.text}>{error ?? AuthCopy.RestoreError}</Text>
        <Button title={AuthCopy.Retry} onPress={() => void restoreSession()} />
      </> : <><ActivityIndicator /><Text style={styles.text}>{AuthCopy.Restoring}</Text></>}
    </View>
  );
}
const styles = StyleSheet.create({
  container: { flex: 1, gap: 16, alignItems: 'center', justifyContent: 'center', padding: 24 },
  text: { color: Colors.light.text },
});
