import { useRouter } from 'expo-router';
import { useCallback, useState } from 'react';
import { Pressable, StyleSheet, Text, useWindowDimensions, View } from 'react-native';
import type { ApiClient } from '@/shared/api/api-client';
import { WorkspaceButton, WorkspaceCard, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { getCalendar, type CalendarLesson } from '../api/get-calendar';
import { addDays, dateKey, lessonMinutes, lessonSummary, lessonTime, shortDate, startOfDay, startOfWeek, timeLabel } from '../../workspace/models/workspace-format';
import { useTutorQuery } from '../../workspace/models/use-tutor-query';
import { lessonRoute } from '../../workspace/models/tutor-routes';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';

const modes = ['Day', 'Week', 'Month'] as const;
const weekday = new Intl.DateTimeFormat('en-GB', { weekday: 'short', day: '2-digit' });
const monthLabel = new Intl.DateTimeFormat('en-GB', { month: 'long', year: 'numeric' });

function overlapsDay(lesson: CalendarLesson, day: Date) {
  return Date.parse(lesson.startsAtUtc) < addDays(day, 1).getTime() && Date.parse(lesson.endsAtUtc) > day.getTime();
}

export function CalendarScreen() {
  const router = useRouter();
  const compact = useWindowDimensions().width < 900;
  const [date, setDate] = useState(() => startOfDay(new Date()));
  const [mode, setMode] = useState<(typeof modes)[number]>('Week');
  const month = new Date(date.getFullYear(), date.getMonth(), 1);
  const start = mode === 'Day' ? date : startOfWeek(mode === 'Month' ? month : date);
  const dayCount = mode === 'Day' ? 1 : mode === 'Week' ? 7 : 42;
  const startTime = start.getTime();
  const load = useCallback((api: ApiClient, signal: AbortSignal) => {
    const from = new Date(startTime);
    return getCalendar(api, from, addDays(from, dayCount), signal);
  }, [startTime, dayCount]);
  const query = useTutorQuery(load);
  const calendar = query.data;
  const days = Array.from({ length: dayCount }, (_, index) => addDays(start, index));
  const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  const range = mode === 'Week' ? `${shortDate(start)}–${shortDate(addDays(start, 6))}` : monthLabel.format(date);

  function move(direction: number) {
    setDate(value => mode === 'Month' ? new Date(value.getFullYear(), value.getMonth() + direction, 1)
      : addDays(value, direction * dayCount));
  }

  function openDay(day: Date) {
    setDate(day);
    setMode('Day');
  }

  return <TutorPage topAligned title={`Calendar · ${mode.toLowerCase()}`} subtitle={`${monthLabel.format(date)} · ${timezone}`}>
    <View style={ui.row}>
      {modes.map(item => <View key={item} style={styles.mode}>
        <WorkspaceButton label={item} primary={item === mode} onPress={() => setMode(item)} />
      </View>)}
      <View style={styles.schedule}>
        <WorkspaceButton label="Schedule lesson" onPress={() => router.push({
          pathname: '/tutor/schedule', params: { date: dateKey(date) },
        })} />
      </View>
    </View>
    <View style={ui.row}>
      <WorkspaceButton label={`Previous ${mode.toLowerCase()}`} onPress={() => move(-1)} />
      <Text style={ui.body}>{mode === 'Day' ? shortDate(date) : range}</Text>
      <WorkspaceButton label={`Next ${mode.toLowerCase()}`} onPress={() => move(1)} />
      <WorkspaceButton label="Today" onPress={() => setDate(startOfDay(new Date()))} />
    </View>
    <QueryFeedback {...query} />
    {calendar && <View style={[styles.grid, (mode === 'Day' || (compact && mode === 'Week')) && styles.list]}>
      {days.map(day => {
        const lessons = calendar.filter(lesson => overlapsDay(lesson, day));
        if (mode === 'Month') return <Pressable key={dateKey(day)} accessibilityRole="button"
          accessibilityLabel={`${shortDate(day)}, ${lessons.length} lessons`} onPress={() => openDay(day)}
          style={[styles.monthDay, day.getMonth() !== month.getMonth() && styles.otherMonth]}>
          <Text style={styles.dayTitle}>{day.getDate()}</Text>
          {lessons.length > 0 && <Text style={styles.small}>{compact ? lessons.length : `${lessons.length} lessons`}</Text>}
        </Pressable>;
        return <View key={dateKey(day)} style={[styles.day, (compact || mode === 'Day') && styles.fullDay]}>
          <Text accessibilityRole="header" style={styles.dayTitle}>{weekday.format(day)}</Text>
          {!lessons.length && <Text style={styles.small}>No lessons</Text>}
          {lessons.map(lesson => <WorkspaceCard key={lesson.lessonId}
            title={compact || mode === 'Day' ? `${lessonTime(lesson)} · ${lesson.studentDisplayName}` : timeLabel(lesson.startsAtUtc)}
            detail={compact || mode === 'Day' ? lessonSummary(lesson)
              : `${lesson.studentDisplayName}\n${lesson.subject}\n${lessonMinutes(lesson)} min · ${lesson.status}`}
            onPress={() => router.push(lessonRoute(lesson))} />)}
        </View>;
      })}
    </View>}
  </TutorPage>;
}

const styles = StyleSheet.create({
  mode: { flexBasis: 90, flexGrow: 1, maxWidth: 180 },
  schedule: { flexBasis: 155, flexGrow: 1, maxWidth: 234 },
  grid: { flexDirection: 'row', flexWrap: 'wrap', alignItems: 'flex-start' },
  list: { flexDirection: 'column', flexWrap: 'nowrap', gap: 8 },
  day: { flex: 1, gap: 10, padding: 8, marginRight: 3, backgroundColor: '#f7f7f7', minWidth: 0 },
  fullDay: { flex: 0, alignSelf: 'stretch', marginRight: 0 },
  monthDay: { width: '14.2857%', minHeight: 58, borderColor: '#ddd', borderWidth: 1, padding: 5, gap: 4 },
  otherMonth: { backgroundColor: '#f0f0f0' },
  dayTitle: { ...ui.body, fontWeight: '600' },
  small: { ...ui.body, fontSize: 12 },
});
