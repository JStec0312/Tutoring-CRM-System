import { useState } from 'react';
import { Text } from 'react-native';
import { useApi } from '@/shared/api/api-context';
import type { ApiClient } from '@/shared/api/api-client';
import { WorkspaceButton, WorkspaceField, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { useAuth } from '@/shared/session/AuthProvider';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';
import { useTutorMutation } from '../../workspace/models/use-tutor-mutation';
import { useTutorQuery } from '../../workspace/models/use-tutor-query';

type Profile = { firstName: string | null; lastName: string | null; phoneNumber: string | null };

function getProfile(api: ApiClient, signal: AbortSignal) {
  return api.get<Profile>('/api/users/me/profile', { signal });
}

export function TutorProfileScreen() {
  const query = useTutorQuery(getProfile);
  return <TutorPage title="Profile & security">
    <QueryFeedback {...query} />
    {query.data && <ProfileForm profile={query.data} />}
    <PasswordForm />
  </TutorPage>;
}

function ProfileForm({ profile }: { profile: Profile }) {
  const api = useApi();
  const [firstName, setFirstName] = useState(profile.firstName ?? '');
  const [lastName, setLastName] = useState(profile.lastName ?? '');
  const [phone, setPhone] = useState(profile.phoneNumber ?? '');
  const [saved, setSaved] = useState(false);
  const mutation = useTutorMutation();

  async function save() {
    setSaved(false);
    await api.put('/api/users/me/profile', {
      firstName: firstName.trim() || null, lastName: lastName.trim() || null, phoneNumber: phone.trim() || null,
    });
    setSaved(true);
  }

  return <>
    <WorkspaceField label="First name" value={firstName} onChangeText={value => { setFirstName(value); setSaved(false); }}
      maxLength={100} editable={!mutation.busy} />
    <WorkspaceField label="Last name" value={lastName} onChangeText={value => { setLastName(value); setSaved(false); }}
      maxLength={100} editable={!mutation.busy} />
    <WorkspaceField label="Phone number" keyboardType="phone-pad" value={phone}
      onChangeText={value => { setPhone(value); setSaved(false); }} maxLength={30} editable={!mutation.busy} />
    {!!mutation.error && <Text accessibilityRole="alert" style={ui.error}>{mutation.error}</Text>}
    {saved && <Text accessibilityLiveRegion="polite" style={ui.body}>Profile saved.</Text>}
    <WorkspaceButton primary label={mutation.busy ? 'Saving…' : 'Save profile'} disabled={mutation.busy}
      onPress={() => void mutation.run(save)} />
  </>;
}

function PasswordForm() {
  const api = useApi();
  const { signOut } = useAuth();
  const [currentPassword, setCurrent] = useState('');
  const [newPassword, setNew] = useState('');
  const [confirm, setConfirm] = useState('');
  const mutation = useTutorMutation();

  async function changePassword() {
    if (newPassword !== confirm) {
      mutation.setError('The new passwords must match.');
      return;
    }
    await api.put('/api/users/me/password', { currentPassword, newPassword });
    setCurrent('');
    setNew('');
    setConfirm('');
    await signOut().catch(() => undefined);
  }

  return <>
    <Text accessibilityRole="header" style={ui.sectionHeading}>Change password</Text>
    <WorkspaceField label="Current password" value={currentPassword} onChangeText={setCurrent} maxLength={256}
      autoComplete="current-password" secureTextEntry editable={!mutation.busy} />
    <WorkspaceField label="New password" value={newPassword} onChangeText={setNew} maxLength={256}
      autoComplete="new-password" secureTextEntry editable={!mutation.busy} />
    <WorkspaceField label="Confirm new password" value={confirm} onChangeText={setConfirm} maxLength={256}
      autoComplete="new-password" secureTextEntry editable={!mutation.busy} />
    <Text style={ui.body}>You will need to sign in again after changing your password.</Text>
    {!!mutation.error && <Text accessibilityRole="alert" style={ui.error}>{mutation.error}</Text>}
    <WorkspaceButton primary label={mutation.busy ? 'Changing…' : 'Change password'}
      disabled={mutation.busy || !currentPassword || !newPassword || !confirm}
      onPress={() => void mutation.run(changePassword)} />
  </>;
}
