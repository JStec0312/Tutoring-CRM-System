import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { Text, View } from 'react-native';
import { useApi } from '@/shared/api/api-context';
import { WorkspaceButton, WorkspaceField, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { getStudents } from '../../students/api/get-students';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';
import { agreementRoute, studentRoute } from '../../workspace/models/tutor-routes';
import { parseRate } from '../../workspace/models/workspace-format';
import { useTutorMutation } from '../../workspace/models/use-tutor-mutation';
import { useTutorQuery } from '../../workspace/models/use-tutor-query';

export function AddAgreementScreen() {
  const { studentId } = useLocalSearchParams<{ studentId: string }>();
  const router = useRouter();
  const api = useApi();
  const query = useTutorQuery(getStudents);
  const student = query.data?.find(student => student.studentId === studentId);
  const [title, setTitle] = useState('');
  const [subject, setSubject] = useState('');
  const [rate, setRate] = useState('');
  const mutation = useTutorMutation();

  async function create() {
    const hourlyRate = parseRate(rate);
    if (Number.isNaN(hourlyRate)) {
      mutation.setError('Enter a valid hourly rate.');
      return;
    }
    const result = await api.post<{ agreementId: string }>(`/api/tutors/me/students/${encodeURIComponent(studentId)}/agreements`, {
      title: title.trim(), subject: subject.trim(), hourlyRate,
    });
    router.replace(agreementRoute(result.agreementId));
  }

  return <TutorPage title="Add agreement" subtitle={student?.displayName}>
    <QueryFeedback {...query} />
    {query.data && !student && <Text style={ui.body}>Student not found.</Text>}
    {student && <>
      <WorkspaceField label="Agreement title" value={title} onChangeText={setTitle} maxLength={100} editable={!mutation.busy} />
      <WorkspaceField label="Subject" value={subject} onChangeText={setSubject} maxLength={100} editable={!mutation.busy} />
      <WorkspaceField label="Hourly rate (PLN)" value={rate} onChangeText={setRate} placeholder="Optional"
        keyboardType="decimal-pad" editable={!mutation.busy} />
    </>}
    {!!mutation.error && <Text accessibilityRole="alert" style={ui.error}>{mutation.error}</Text>}
    <View style={ui.row}>
      {student && <WorkspaceButton primary label="Create agreement" disabled={mutation.busy || !title.trim() || !subject.trim()}
        onPress={() => void mutation.run(create)} />}
      <WorkspaceButton label="Cancel" disabled={mutation.busy} onPress={() => router.dismissTo(studentRoute(studentId))} />
    </View>
  </TutorPage>;
}
