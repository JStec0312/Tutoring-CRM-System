import { View, StyleSheet } from 'react-native';
import { getProfile, ProfileForm, PasswordForm } from '@/shared/components/account-profile';
import { WorkspaceButton } from '@/shared/components/ui/workspace-controls';
import { useAuth } from '@/shared/session/AuthProvider';
import { useWorkspaceQuery } from '@/shared/models/use-workspace-query';
import { StudentPage, StudentQueryFeedback } from '../../workspace/components/student-workspace';

export function StudentProfileScreen() {
  const { signOut } = useAuth();
  const query = useWorkspaceQuery(getProfile);
  return <StudentPage code="C02" title="Profile & security">
    <StudentQueryFeedback {...query} />
    <View style={styles.form}>
      {query.data && <ProfileForm profile={query.data} />}
      <PasswordForm />
      <WorkspaceButton label="Sign out" onPress={() => void signOut().catch(() => console.warn('Could not sign out on the server.'))} />
    </View>
  </StudentPage>;
}

const styles = StyleSheet.create({ form: { width: '100%', maxWidth: 440, gap: 20 } });
