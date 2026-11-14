import { useLocalSearchParams, useRouter } from 'expo-router';
import { useRef, useState } from 'react';
import { Text, useWindowDimensions, View } from 'react-native';
import { useApi } from '@/shared/api/api-context';
import { WorkspaceButton, WorkspaceCard, WorkspaceColumns, WorkspaceField, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { addStudent, inviteStudent } from '../api/add-student';
import { validateStudentForm } from '../models/validate-student-form';
import { errorMessage } from '../../workspace/models/use-tutor-query';
import { agreementRoute } from '../../workspace/models/tutor-routes';
import { TutorPage } from '../../workspace/components/tutor-workspace';

export function AddStudentScreen() {
  const router = useRouter();
  const { mode } = useLocalSearchParams<{ mode?: string }>();
  const compact = useWindowDimensions().width < 900;
  const api = useApi();
  const [invite, setInvite] = useState(mode === 'invite');
  const [displayName, setDisplayName] = useState('');
  const [email, setEmail] = useState('');
  const [title, setTitle] = useState('');
  const [subject, setSubject] = useState('');
  const [rate, setRate] = useState('');
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);
  const [invitationSent, setInvitationSent] = useState(false);
  const inFlight = useRef(false);

  async function submit() {
    if (inFlight.current) return;
    const input = validateStudentForm(invite ? { mode: 'invite', email } : { mode: 'manual', displayName }, title, subject, rate);
    if (input.error) {
      setError(input.error);
      return;
    }
    const { agreement } = input;
    inFlight.current = true;
    setSaving(true);
    setError('');
    try {
      if (invite) {
        await inviteStudent(api, { ...agreement, email: email.trim() });
        setInvitationSent(true);
      } else {
        const result = await addStudent(api, { ...agreement, displayName: displayName.trim() });
        router.replace(agreementRoute(result.agreementId));
      }
    } catch (error) {
      setError(errorMessage(error));
    } finally {
      inFlight.current = false;
      setSaving(false);
    }
  }

  if (invitationSent) return <TutorPage title="Invitation sent">
    <WorkspaceCard title={email.trim()} detail={`${subject.trim()} · Awaiting acceptance`} />
    <WorkspaceButton primary label="Back to students" onPress={() => router.replace('/tutor/students')} />
  </TutorPage>;

  return <TutorPage title={invite ? 'Invite student by email' : 'Add student manually'}>
    <WorkspaceColumns compact={compact}>
      <View style={compact ? ui.stack : ui.main}>
        {invite
          ? <WorkspaceField label="Student email" value={email} onChangeText={setEmail} maxLength={320}
              keyboardType="email-address" autoCapitalize="none" editable={!saving} />
          : <WorkspaceField label="Student name" value={displayName} onChangeText={setDisplayName} maxLength={100} editable={!saving} />}
        <WorkspaceField label="Agreement title" value={title} onChangeText={setTitle} maxLength={100} editable={!saving} />
        <WorkspaceField label="Subject" value={subject} onChangeText={setSubject} maxLength={100} editable={!saving} />
        <WorkspaceField label="Hourly rate (PLN)" value={rate} onChangeText={setRate}
          placeholder="Optional" keyboardType="decimal-pad" editable={!saving} />
        {!!error && <Text accessibilityRole="alert" style={ui.error}>{error}</Text>}
        <WorkspaceButton primary disabled={saving} label={saving ? 'Saving…' : invite ? 'Send invitation' : 'Create student & agreement'}
          onPress={() => void submit()} />
        <WorkspaceButton label="Cancel" disabled={saving} onPress={() => router.navigate('/tutor/students')} />
      </View>
      <View style={compact ? ui.stack : ui.aside}>
        <Text accessibilityRole="header" style={ui.sectionHeading}>{invite ? 'Add manually instead' : 'Invite instead'}</Text>
        <Text style={ui.body}>{invite ? 'Create a student profile and manage it yourself.' :
          'Send an invitation when the student should connect their own account.'}</Text>
        <WorkspaceButton label={invite ? 'Add student manually' : 'Invite by email'} disabled={saving} onPress={() => {
          setInvite(!invite);
          setError('');
        }} />
      </View>
    </WorkspaceColumns>
  </TutorPage>;
}
