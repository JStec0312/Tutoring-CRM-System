import { useRouter } from 'expo-router';
import { Text, View } from 'react-native';
import { WorkspaceButton, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { useWorkspaceQuery } from '@/shared/models/use-workspace-query';
import { StudentPage, StudentQueryFeedback, studentStyles } from '../../workspace/components/student-workspace';
import { StudentTable } from '../../workspace/components/student-table';
import { studentLessonRoute } from '../../workspace/models/student-routes';
import { agreementLabel, tutorName } from '../../tutors/api/agreements';
import { outstandingByCurrency } from '../../payments/api/charges';
import { money, shortDate, timeLabel } from '@/features/tutor/workspace/models/workspace-format';
import { MaterialDownloadButton } from '../../materials/components/material-download-button';
import { loadStudentOverview } from '../api/load-overview';

export function StudentOverviewScreen() {
  const router = useRouter();
  const query = useWorkspaceQuery(loadStudentOverview);
  const next = query.data?.next;
  return <StudentPage code="S01" title="Your learning overview">
    <Text style={ui.body}>Welcome to your learning workspace.</Text>
    <StudentQueryFeedback {...query} />
    {query.data && <>
      <View style={studentStyles.notice}>
        {next && <Text style={studentStyles.noticeText}>Next lesson · {next.subject} with {tutorName(next)} · {shortDate(next.startsAtUtc)}, {timeLabel(next.startsAtUtc)}–{timeLabel(next.endsAtUtc)}</Text>}
        {!next && <Text style={studentStyles.noticeText}>No upcoming lessons in the next year.</Text>}
      </View>
      <View style={ui.row}>
        {next && <WorkspaceButton primary label="View lesson" onPress={() => router.push(studentLessonRoute(next.lessonId))} />}
        <WorkspaceButton label="Open calendar" onPress={() => router.navigate('/student/calendar')} />
      </View>
      <StudentTable label="Tutoring agreements" headings={['Tutoring agreement', 'Next lesson', 'Outstanding']}
        rows={query.data.summaries.map(({ agreement, charges, next: lesson }) => {
          let nextLabel = 'Not scheduled';
          if (lesson) nextLabel = `${shortDate(lesson.startsAtUtc)} · ${timeLabel(lesson.startsAtUtc)}`;
          const outstanding = outstandingByCurrency(charges).map(([currency, amount]) => money(amount, currency)).join(' + ');
          return { key: agreement.agreementId, cells: [agreementLabel(agreement), nextLabel, outstanding || 'Nothing outstanding'] };
        })} />
      {query.data.summaries.length === 0 && <Text style={ui.body}>No tutoring agreements yet. Ask your tutor for an invitation.</Text>}
      <Text role="heading" aria-level={2} style={ui.sectionHeading}>Latest materials</Text>
      <StudentTable label="Latest learning materials" headings={['Material', 'Agreement', 'Action']}
        rows={query.data.summaries.flatMap(({ agreement, materials }) => materials.slice(0, 3).map(material => ({
          key: material.materialId, cells: [material.title, agreementLabel(agreement), <MaterialDownloadButton key={material.materialId} material={material} />],
        })))} />
      {query.data.summaries.every(summary => summary.materials.length === 0) && <Text style={ui.body}>No shared materials yet.</Text>}
    </>}
  </StudentPage>;
}
