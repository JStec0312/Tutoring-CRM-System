import { useLocalSearchParams, useRouter } from 'expo-router';
import { Text } from 'react-native';
import { WorkspaceButton, WorkspaceCard, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { getStudents } from '../api/get-students';
import { agreementRoute } from '../../workspace/models/tutor-routes';
import { money } from '../../workspace/models/workspace-format';
import { useTutorQuery } from '../../workspace/models/use-tutor-query';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';

export function StudentProfileScreen() {
  const { studentId } = useLocalSearchParams<{ studentId: string }>();
  const router = useRouter();
  const query = useTutorQuery(getStudents);
  const student = query.data?.find(item => item.studentId === studentId);
  return <TutorPage title={student?.displayName ?? 'Student profile'}>
    <QueryFeedback {...query} />
    {query.data && !student && <Text style={ui.body}>Student not found.</Text>}
    {student && <>
      <WorkspaceButton primary label="Add agreement" onPress={() => router.push({
        pathname: '/tutor/students/[studentId]/add-agreement', params: { studentId },
      })} />
      <Text accessibilityRole="header" style={ui.sectionHeading}>Tutoring agreements</Text>
      {student.agreements.map(agreement => <WorkspaceCard key={agreement.tutoringAgreementId}
        title={agreement.subject}
        detail={[agreement.title, agreement.status, agreement.hourlyRate != null && agreement.currency &&
          `${money(agreement.hourlyRate, agreement.currency)}/hour`].filter(Boolean).join(' · ')}
        onPress={() => router.push(agreementRoute(agreement.tutoringAgreementId))} />)}
      <WorkspaceButton label="Statistics & history" onPress={() => router.push({
        pathname: '/tutor/students/[studentId]/statistics', params: { studentId },
      })} />
    </>}
    <WorkspaceButton label="Back to students" onPress={() => router.navigate('/tutor/students')} />
  </TutorPage>;
}
