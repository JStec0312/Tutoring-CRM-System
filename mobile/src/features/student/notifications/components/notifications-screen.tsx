import { useRouter } from 'expo-router';
import { Text, View } from 'react-native';
import { WorkspaceButton, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { useWorkspaceQuery } from '@/shared/models/use-workspace-query';
import { shortDate, timeLabel } from '@/features/tutor/workspace/models/workspace-format';
import { StudentPage, StudentQueryFeedback, studentStyles } from '../../workspace/components/student-workspace';
import { StudentTable } from '../../workspace/components/student-table';
import { studentLessonRoute } from '../../workspace/models/student-routes';
import { getStudentNotifications } from '../api/get-notifications';

export function StudentNotificationsScreen() {
  const router = useRouter();
  const query = useWorkspaceQuery(getStudentNotifications);
  return <StudentPage code="C01" title="Notifications">
    <StudentQueryFeedback {...query} />
    {query.data && <StudentTable label="Lesson notifications" headings={['Notification', 'Lesson date', 'Action']}
      rows={query.data.map(lesson => {
        let label = 'Lesson reminder';
        if (lesson.status === 'Cancelled') label = 'Lesson cancelled';
        return { key: lesson.lessonId, cells: [`${label} · ${lesson.subject}`,
          `${shortDate(lesson.startsAtUtc)}, ${timeLabel(lesson.startsAtUtc)}`,
          <WorkspaceButton textOnly key={lesson.lessonId} label="Open lesson →" onPress={() => router.push(studentLessonRoute(lesson.lessonId))} />] };
      })} />}
    {query.data?.length === 0 && <Text style={ui.body}>No lesson notifications.</Text>}
    <View style={studentStyles.notice}><Text style={studentStyles.noticeText}>Upcoming lessons and cancellations within a week of today. Open a lesson for its current details.</Text></View>
  </StudentPage>;
}
