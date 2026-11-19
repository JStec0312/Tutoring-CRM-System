import { useRouter } from 'expo-router';
import { Text } from 'react-native';
import { WorkspaceButton, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { LessonFilters, LessonList, LessonPagination } from '@/shared/lessons/components/lesson-list';
import { useLessonPage } from '@/shared/lessons/models/use-lesson-page';
import { StudentPage, StudentQueryFeedback } from '../../workspace/components/student-workspace';
import { studentLessonRoute } from '../../workspace/models/student-routes';

export function StudentLessonsScreen() {
  const router = useRouter();
  const query = useLessonPage(false);
  return <StudentPage code="S05" title="My lessons">
    <Text style={ui.body}>All your lessons in one place — when, with whom and what you are learning.</Text>
    <LessonFilters view={query.view} onSelect={query.selectView} />
    {query.agreementId && <WorkspaceButton label="Show all agreements" onPress={query.clearAgreement} />}
    <StudentQueryFeedback {...query} />
    {query.data && <>
      <LessonList lessons={query.data.items} tutorOnly={false} onOpen={lesson => router.push(studentLessonRoute(lesson.lessonId))}
        onCancel={lesson => router.push({ pathname: '/student/lessons/[lessonId]/cancel', params: { lessonId: lesson.lessonId } })} />
      <LessonPagination data={query.data} onSelect={query.selectPage} />
    </>}
  </StudentPage>;
}
