import { useCallback, useState } from 'react';
import { useRouter } from 'expo-router';
import { Pressable, StyleSheet, Text, useWindowDimensions, View } from 'react-native';
import type { ApiClient } from '@/shared/api/api-client';
import { WorkspaceButton, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { useWorkspaceQuery } from '@/shared/models/use-workspace-query';
import { addDays, dateKey, lessonMinutes, shortDate, startOfDay, startOfWeek, timeLabel } from '@/features/tutor/workspace/models/workspace-format';
import { StudentPage, StudentQueryFeedback, studentStyles } from '../../workspace/components/student-workspace';
import { useStudentAgreements } from '../../workspace/models/use-student-agreements';
import { studentLessonRoute } from '../../workspace/models/student-routes';
import { tutorName } from '../../tutors/api/agreements';
import { getStudentCalendar } from '../api/get-student-calendar';

const modes = ['Day', 'Week', 'Month'] as const;
const weekday = new Intl.DateTimeFormat('en-GB', { weekday: 'short', day: '2-digit' });

export function StudentCalendarScreen() {
  const router = useRouter();
  const compact = useWindowDimensions().width < 900;
  const [date, setDate] = useState(() => startOfDay(new Date()));
  const [mode, setMode] = useState<(typeof modes)[number]>('Week');
  const [agreementId, setAgreementId] = useState('');
  const agreements = useStudentAgreements();
  let start = date;
  let dayCount = 1;
  if (mode === 'Week') {
    start = startOfWeek(date);
    dayCount = 7;
  } else if (mode === 'Month') {
    start = startOfWeek(new Date(date.getFullYear(), date.getMonth(), 1));
    dayCount = 42;
  }
  const startTime = start.getTime();
  const load = useCallback((api: ApiClient, signal: AbortSignal) => {
    const from = new Date(startTime);
    return getStudentCalendar(api, from, addDays(from, dayCount), signal);
  }, [startTime, dayCount]);
  const query = useWorkspaceQuery(load);
  const calendarLessons = query.data;
  const days = Array.from({ length: dayCount }, (_, index) => addDays(start, index));
  const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;

  function move(direction: number) {
    if (mode === 'Month') {
      setDate(new Date(date.getFullYear(), date.getMonth() + direction, 1));
    } else {
      setDate(addDays(date, direction * dayCount));
    }
  }

  return <StudentPage code="S02" title="My calendar">
    <View style={studentStyles.notice}><Text style={studentStyles.noticeText}>
      {shortDate(start)}–{shortDate(addDays(start, dayCount - 1))} {date.getFullYear()} · {timezone}
    </Text></View>
    <View style={ui.row}>
      {modes.map(item => <WorkspaceButton key={item} label={item} primary={item === mode} onPress={() => setMode(item)} />)}
      <WorkspaceButton label="Previous" onPress={() => move(-1)} />
      <WorkspaceButton label="Today" onPress={() => setDate(startOfDay(new Date()))} />
      <WorkspaceButton label="Next" onPress={() => move(1)} />
    </View>
    <View style={ui.row}>
      <WorkspaceButton label="All tutors" primary={!agreementId} onPress={() => setAgreementId('')} />
      {agreements.data?.map(agreement => <WorkspaceButton key={agreement.agreementId}
        label={`${tutorName(agreement)} · ${agreement.subject}`} primary={agreementId === agreement.agreementId}
        onPress={() => setAgreementId(agreement.agreementId)} />)}
    </View>
    <StudentQueryFeedback {...agreements} />
    <StudentQueryFeedback {...query} />
    {query.data && <View style={[styles.grid, mode !== 'Month' && compact && styles.list]}>
      {days.map(day => {
        const lessons = (calendarLessons ?? []).filter(lesson => (!agreementId || lesson.tutoringAgreementId === agreementId)
          && Date.parse(lesson.startsAtUtc) < addDays(day, 1).getTime() && Date.parse(lesson.endsAtUtc) > day.getTime());
        if (mode === 'Month') {
          return <Pressable key={dateKey(day)} accessibilityRole="button"
            accessibilityLabel={`${weekday.format(day)}, ${lessons.length} lessons`}
            onPress={() => { setDate(day); setMode('Day'); }}
            style={[styles.monthDay, day.getMonth() !== date.getMonth() && styles.otherMonth]}>
            <Text style={styles.dayTitle}>{day.getDate()}</Text>
            <Text style={ui.body}>{lessons.length} lessons</Text>
          </Pressable>;
        }
        return <View key={dateKey(day)} style={[styles.day, (compact || mode === 'Day') && styles.fullDay]}>
          <Text role="heading" aria-level={2} style={styles.dayTitle}>{weekday.format(day)}</Text>
          {lessons.length === 0 && <Text style={ui.body}>No lessons</Text>}
          {lessons.map(lesson => <Pressable key={lesson.lessonId} accessibilityRole="button"
            onPress={() => router.push(studentLessonRoute(lesson.lessonId))} style={styles.lesson}>
            <Text style={ui.body}>{timeLabel(lesson.startsAtUtc)}–{timeLabel(lesson.endsAtUtc)}</Text>
            <Text style={ui.body}>{tutorName(lesson)}</Text>
            <Text style={ui.body}>{lesson.subject}</Text>
            <Text style={ui.body}>{lessonMinutes(lesson)} min · {lesson.status}</Text>
          </Pressable>)}
        </View>;
      })}
    </View>}
    <View style={studentStyles.notice}><Text style={studentStyles.noticeText}>Open a lesson to see details or cancel it with a reason.</Text></View>
  </StudentPage>;
}

const styles = StyleSheet.create({
  grid: { flexDirection: 'row', flexWrap: 'wrap', alignItems: 'stretch' },
  list: { flexDirection: 'column', gap: 8 },
  day: { flex: 1, minWidth: 0, minHeight: 430, padding: 10, borderWidth: 0.5, borderColor: '#d1d1d1', gap: 12 },
  fullDay: { flex: 0, width: '100%', minHeight: 100 },
  dayTitle: { ...ui.body, fontWeight: '600' },
  lesson: { paddingHorizontal: 8, paddingVertical: 12, backgroundColor: '#d9d9d9', gap: 4, minHeight: 44 },
  monthDay: { width: '14.2857%', minHeight: 85, borderWidth: 0.5, borderColor: '#d1d1d1', padding: 6 },
  otherMonth: { backgroundColor: '#eee' },
});
