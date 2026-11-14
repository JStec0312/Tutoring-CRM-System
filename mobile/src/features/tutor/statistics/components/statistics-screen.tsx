import { useRouter } from 'expo-router';
import { useState } from 'react';
import { Text, View } from 'react-native';
import { WorkspaceButton, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { getWorkspaceStatistics } from '../api/workspace-statistics';
import { attendanceLabel, durationLabel, periodStart, periods, type StatisticsPeriod } from '../models/statistics-period';
import { chargeTotals } from '../../payments/api/lesson-charges';
import { lessonMinutes, money } from '../../workspace/models/workspace-format';
import { useTutorQuery } from '../../workspace/models/use-tutor-query';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';
import { WorkspaceMetrics, WorkspaceTable } from '../../workspace/components/workspace-data';

export function StatisticsScreen() {
  const router = useRouter();
  const query = useTutorQuery(getWorkspaceStatistics);
  const [period, setPeriod] = useState<StatisticsPeriod>('Month');
  const [currency, setCurrency] = useState('');
  const start = periodStart(period).getTime();
  const today = new Date().toISOString().slice(0, 10);
  const through = Date.parse(today) + 86_400_000;
  const lessons = query.data?.lessons.filter(lesson => {
    const starts = Date.parse(lesson.startsAtUtc);
    return starts >= start && starts < through;
  }) ?? [];
  const completed = lessons.filter(lesson => lesson.status === 'Completed');
  const charges = query.data?.charges.filter(charge => {
    const starts = Date.parse(charge.startsAtUtc);
    return !charge.paidAtUtc && starts >= start && starts < through && (!currency || charge.currency === currency);
  }) ?? [];
  const revenueKey = { Week: 'weekly', Month: 'monthly', Year: 'yearly' } as const;
  const revenue = query.data?.revenue.filter(item => !currency || item.currency === currency)
    .map(item => money(item[revenueKey[period]], item.currency)).join(' + ') || '0';
  const currencies = [...new Set([
    ...query.data?.revenue.map(row => row.currency) ?? [],
    ...query.data?.charges.map(row => row.currency) ?? [],
  ])].sort();

  return <TutorPage title="Statistics">
    <View style={ui.row}>
      {periods.map(value => <WorkspaceButton key={value} label={value} primary={period === value} onPress={() => setPeriod(value)} />)}
    </View>
    <QueryFeedback {...query} />
    {query.data && <>
      {currencies.length > 1 && <View style={ui.row}>
        <WorkspaceButton label="All currencies" primary={!currency} onPress={() => setCurrency('')} />
        {currencies.map(value => <WorkspaceButton key={value} label={value} primary={currency === value} onPress={() => setCurrency(value)} />)}
      </View>}
      <WorkspaceMetrics items={[
        { label: 'Completed lessons', value: String(completed.length) },
        { label: 'Teaching time', value: durationLabel(completed.reduce((sum, lesson) => sum + lessonMinutes(lesson), 0)) },
        { label: 'Revenue', value: revenue },
      ]} />
      {!query.data.students.length && <Text style={ui.body}>No students yet.</Text>}
      <WorkspaceTable headers={['Student', 'Completed / missed', 'Attendance', 'Outstanding']} rows={query.data.students.map(student => {
        const agreementIds = new Set(student.agreements.map(agreement => agreement.tutoringAgreementId));
        const history = lessons.filter(lesson => agreementIds.has(lesson.tutoringAgreementId));
        const completedCount = history.filter(lesson => lesson.status === 'Completed').length;
        const missed = history.filter(lesson => lesson.status === 'Missed').length;
        const outstanding = chargeTotals(charges.filter(charge => agreementIds.has(charge.agreementId)))
          .map(([code, amount]) => money(amount, code)).join(' + ') || '0';
        return {
          key: student.studentId,
          cells: [student.displayName, `${completedCount} / ${missed}`, attendanceLabel(completedCount, missed), outstanding],
          onPress: () => router.push({ pathname: '/tutor/students/[studentId]/statistics', params: { studentId: student.studentId, period } }),
        };
      })} />
    </>}
  </TutorPage>;
}
