import { useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useState } from 'react';
import { Text, View } from 'react-native';
import type { ApiClient } from '@/shared/api/api-client';
import { WorkspaceButton, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { getStudentHistory } from '../../agreement/api/agreement';
import { getStudents } from '../../students/api/get-students';
import { chargeTotals, getLessonCharges } from '../../payments/api/lesson-charges';
import { WorkspaceMetrics, WorkspaceTable } from '../../workspace/components/workspace-data';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';
import { lessonRoute, studentRoute } from '../../workspace/models/tutor-routes';
import { useTutorQuery } from '../../workspace/models/use-tutor-query';
import { lessonMinutes, money, shortDate } from '../../workspace/models/workspace-format';
import { attendanceLabel, durationLabel, periods, periodStart, type StatisticsPeriod } from '../models/statistics-period';

export function StudentStatisticsScreen() {
  const { studentId, period: initialPeriod } = useLocalSearchParams<{ studentId: string; period?: StatisticsPeriod }>();
  const [period, setPeriod] = useState<StatisticsPeriod>(initialPeriod && periods.includes(initialPeriod) ? initialPeriod : 'Month');
  const router = useRouter();
  const load = useCallback(async (api: ApiClient, signal: AbortSignal) => {
    const from = periodStart(period).toISOString().slice(0, 10);
    const to = new Date().toISOString().slice(0, 10);
    const [students, history, charges, counts] = await Promise.all([
      getStudents(api, signal), getStudentHistory(api, studentId, signal), getLessonCharges(api, signal),
      api.get<{ lessons: { scheduledCount: number; completedCount: number; cancelledCount: number; missedCount: number } }>(
        `/api/tutors/me/students/${encodeURIComponent(studentId)}/statistics?from=${from}&to=${to}`, { signal }),
    ]);
    return {
      student: students.find(student => student.studentId === studentId),
      history: history.filter(lesson => Date.parse(lesson.startsAtUtc) >= Date.parse(from)),
      charges: charges.filter(charge => Date.parse(charge.startsAtUtc) >= Date.parse(from)),
      counts: counts.lessons,
    };
  }, [period, studentId]);
  const query = useTutorQuery(load);
  const data = query.data;

  return <TutorPage title="Student statistics" subtitle={data?.student?.displayName}>
    <View style={ui.row}>{periods.map(value => <WorkspaceButton key={value} label={value} primary={period === value}
      onPress={() => setPeriod(value)} />)}</View>
    <QueryFeedback {...query} />
    {data?.student && <>
      <WorkspaceMetrics items={[
        { label: 'Completed / total', value: `${data.counts.completedCount} / ${data.counts.scheduledCount +
          data.counts.completedCount + data.counts.cancelledCount + data.counts.missedCount}` },
        { label: 'Attendance', value: attendanceLabel(data.counts.completedCount, data.counts.missedCount) },
        { label: 'Cancelled', value: String(data.counts.cancelledCount) },
      ]} />
      <WorkspaceTable headers={['Agreement', 'Teaching time', 'Receivables']} rows={data.student.agreements.map(agreement => {
        const id = agreement.tutoringAgreementId;
        const minutes = data.history.filter(lesson => lesson.tutoringAgreementId === id && lesson.status === 'Completed')
          .reduce((sum, lesson) => sum + lessonMinutes(lesson), 0);
        const unpaid = data.charges.filter(charge => charge.agreementId === id && !charge.paidAtUtc);
        return { key: id, cells: [agreement.title, durationLabel(minutes), chargeTotals(unpaid)
          .map(([currency, amount]) => money(amount, currency)).join(' + ') || '0'] };
      })} />
      <Text accessibilityRole="header" style={ui.sectionHeading}>Lesson history</Text>
      {!data.history.length && <Text style={ui.body}>No past lessons in this period.</Text>}
      <WorkspaceTable headers={['Date / topic', 'Agreement', 'Status']} rows={data.history.map(lesson => ({
        key: lesson.lessonId, cells: [`${shortDate(lesson.startsAtUtc)} · ${lesson.topic || lesson.subject}`, lesson.agreementTitle, lesson.status],
        onPress: () => router.push(lessonRoute(lesson)),
      }))} />
    </>}
    <WorkspaceButton label="Back to student" onPress={() => router.navigate(studentRoute(studentId))} />
  </TutorPage>;
}
