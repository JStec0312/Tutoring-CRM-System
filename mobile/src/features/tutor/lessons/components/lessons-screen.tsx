import { useRouter } from 'expo-router';
import { StyleSheet, Text } from 'react-native';
import { useApi } from '@/shared/api/api-context';
import { WorkspaceButton, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { LessonFilters, LessonList, LessonPagination } from '@/shared/lessons/components/lesson-list';
import { useLessonPage } from '@/shared/lessons/models/use-lesson-page';
import { useWorkspaceMutation } from '@/shared/models/use-workspace-mutation';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';
import { lessonRoute } from '../../workspace/models/tutor-routes';
import { recordPayments } from '../../payments/api/lesson-charges';

export function TutorLessonsScreen() {
  const router = useRouter();
  const api = useApi();
  const query = useLessonPage(true);
  const mutation = useWorkspaceMutation();
  async function markPaid(chargeIds: string[]) {
    await recordPayments(api, chargeIds);
    query.reload();
  }
  return <TutorPage topAligned eyebrow="T11 / TUTOR / LESSONS" contentContainerStyle={styles.content} headingStyle={styles.heading} title="My lessons" subtitle="All your lessons in one place — when, with which student and what you are teaching.">
    <LessonFilters view={query.view} onSelect={query.selectView} />
    {query.agreementId && <WorkspaceButton label="Show all agreements" onPress={query.clearAgreement} />}
    <QueryFeedback {...query} />
    {!!mutation.error && <Text role="alert" style={ui.error}>{mutation.error}</Text>}
    {query.data && <>
      <LessonList lessons={query.data.items} tutorOnly onOpen={lesson => router.push(lessonRoute(lesson))}
        onCancel={lesson => router.push({ pathname: '/tutor/lessons/[lessonId]/cancel', params: { lessonId: lesson.lessonId } })}
        renderActions={lesson => {
          const unpaid = lesson.charges.filter(charge => !charge.paidAtUtc);
          if (unpaid.length === 0) return null;
          return <WorkspaceButton primary label="Mark as paid" accessibilityLabel={`Mark ${lesson.subject} as paid`}
            disabled={mutation.busy} onPress={() => void mutation.run(() => markPaid(unpaid.map(charge => charge.chargeId)))} />;
        }} />
      <LessonPagination data={query.data} onSelect={query.selectPage} />
    </>}
  </TutorPage>;
}

const styles = StyleSheet.create({
  content: { maxWidth: 950, gap: 20 },
  heading: { fontSize: 30, lineHeight: 44 },
});
