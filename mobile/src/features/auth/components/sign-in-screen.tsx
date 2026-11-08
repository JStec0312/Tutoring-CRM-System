import { useState } from 'react';
import { Button, KeyboardAvoidingView, Platform, StyleSheet, Text, TextInput } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { errorMessage } from '@/shared/api/http';
import { useSession } from '@/shared/session/session-context';
import { Colors } from '@/shared/theme/theme';
import { AuthCopy } from '../models/auth-copy';

export function SignInScreen() {
  const { signIn, signOut, error: sessionError } = useSession();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  async function submit() {
    if (pending) return;
    if (!email.trim() || !password) { setError(AuthCopy.Required); return; }
    setPending(true);
    setError(null);
    try { await signIn({ email: email.trim(), password }); setPassword(''); }
    catch (failure) { setError(errorMessage(failure)); }
    finally { setPending(false); }
  }
  return (
    <SafeAreaView style={styles.container}>
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined} style={styles.form}>
        <Text style={styles.title}>{AuthCopy.Title}</Text>
        <TextInput accessibilityLabel={AuthCopy.Email} placeholder={AuthCopy.Email} value={email}
          onChangeText={setEmail} autoCapitalize="none" autoComplete="email" keyboardType="email-address"
          editable={!pending} style={styles.input} />
        <TextInput accessibilityLabel={AuthCopy.Password} placeholder={AuthCopy.Password} value={password}
          onChangeText={setPassword} secureTextEntry autoComplete="current-password" editable={!pending}
          onSubmitEditing={() => void submit()} style={styles.input} />
        {error && <Text accessibilityRole="alert" style={styles.text}>{error}</Text>}
        {sessionError && <>
          <Text accessibilityRole="alert" style={styles.text}>{sessionError}</Text>
          <Button title={AuthCopy.SignOut} disabled={pending} onPress={() => void signOut().catch(() => undefined)} />
        </>}
        <Button title={pending ? AuthCopy.SigningIn : AuthCopy.SignIn} disabled={pending} onPress={() => void submit()} />
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, justifyContent: 'center', padding: 24 },
  form: { width: '100%', maxWidth: 420, alignSelf: 'center', gap: 16 },
  title: { fontSize: 26, fontWeight: '600', color: Colors.light.text },
  text: { color: Colors.light.text },
  input: { borderWidth: 1, borderColor: '#777', borderRadius: 6, padding: 12, color: Colors.light.text },
});
