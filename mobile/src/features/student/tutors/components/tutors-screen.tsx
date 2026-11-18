import { useCallback } from 'react';
import { useRouter } from 'expo-router';
import { Text } from 'react-native';
import type { ApiClient } from '@/shared/api/api-client';
import { WorkspaceButton, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { useWorkspaceQuery } from '@/shared/models/use-workspace-query';
import { StudentPage, StudentQueryFeedback } from '../../workspace/components/student-workspace';
import { StudentTable } from '../../workspace/components/student-table';
import { useStudentAgreements } from '../../workspace/models/use-student-agreements';
import { studentLessonRoute } from '../../workspace/models/student-routes';
import { getStudentLessons } from '../../lesson/api/lessons';
import { money, shortDate, lessonMinutes } from '@/features/tutor/workspace/models/workspace-format';
import { tutorName } from '../api/agreements';

export function StudentTutorsScreen() {
  const router = useRouter();
  const agreements = useStudentAgreements();
  const agreementId = agreements.agreement?.agreementId;
  const load = useCallback(async (api: ApiClient, signal: AbortSignal) => {
    if (!agreementId) return [];
    return getStudentLessons(api, agreementId, signal);
  }, [agreementId]);
  const lessons = useWorkspaceQuery(load);
  return <StudentPage code="S06" title="My tutors & lesson history">
    <StudentQueryFeedback {...agreements} />
    {agreements.data && <StudentTable label="My tutors" headings={['Tutor', 'Agreement', 'Status']}
      rows={agreements.data.map(agreement => {
        let rate = 'Rate not set';
        if (agreement.hourlyRate != null && agreement.currency) rate = `${money(agreement.hourlyRate, agreement.currency)}/h`;
        return { key: agreement.agreementId, cells: [tutorName(agreement),
          <WorkspaceButton key={agreement.agreementId} label={`${agreement.subject} · ${rate}`} primary={agreementId === agreement.agreementId}
            onPress={() => agreements.selectAgreement(agreement.agreementId)} />, agreement.status] };
      })} />}
    {agreements.data?.length === 0 && <Text style={ui.body}>You have no tutors yet. Ask your tutor for an invitation.</Text>}
    {agreements.agreement && <>
      <Text role="heading" aria-level={2} style={ui.sectionHeading}>{agreements.agreement.subject} / Lesson history</Text>
      <StudentQueryFeedback {...lessons} />
      {lessons.data && <StudentTable label="Lesson history" headings={['Date / topic', 'Duration', 'Status']}
        rows={lessons.data.map(lesson => ({ key: lesson.lessonId, cells: [
          <WorkspaceButton textOnly key={lesson.lessonId} label={`${shortDate(lesson.startsAtUtc)} / ${lesson.plannedTopic || lesson.subject}`}
            onPress={() => router.push(studentLessonRoute(lesson.lessonId))} />,
          `${lessonMinutes(lesson)} min`, [lesson.status, lesson.cancellationReason].filter(Boolean).join(' · '),
        ] }))} />}
      {lessons.data?.length === 0 && <Text style={ui.body}>No lessons in this agreement.</Text>}
    </>}
  </StudentPage>;
}
