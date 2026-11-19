import type { ReactNode } from 'react';
import { Pressable, StyleSheet, Text, useWindowDimensions, View } from 'react-native';
import { WorkspaceButton, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { lessonMinutes, money, shortDate, timeLabel } from '@/features/tutor/workspace/models/workspace-format';
import type { LessonListItem, LessonPage, LessonView } from '../api/get-lesson-page';

export function LessonFilters({ view, onSelect }: { view: LessonView; onSelect: (view: LessonView) => void }) {
  return <View style={ui.row}>
    <WorkspaceButton label="All lessons" primary={view === 'all'} onPress={() => onSelect('all')} />
    <WorkspaceButton label="Upcoming" primary={view === 'upcoming'} onPress={() => onSelect('upcoming')} />
    <WorkspaceButton label="Past lessons" primary={view === 'past'} onPress={() => onSelect('past')} />
  </View>;
}

export function paymentLabel(lesson: LessonListItem) {
  if (lesson.charges.length === 0) return 'Not charged';
  return lesson.charges.map(charge => {
    let status = 'Unpaid';
    if (charge.paidAtUtc) status = 'Paid';
    return `${money(charge.amount, charge.currency)} · ${status}`;
  }).join(' / ');
}

export function LessonList({ lessons, tutorOnly, onOpen, onCancel, renderActions }: {
  lessons: LessonListItem[];
  tutorOnly: boolean;
  onOpen: (lesson: LessonListItem) => void;
  onCancel: (lesson: LessonListItem) => void;
  renderActions?: (lesson: LessonListItem) => ReactNode;
}) {
  const compact = useWindowDimensions().width < 1000;
  let participantHeading = 'With whom';
  if (tutorOnly) participantHeading = 'Student';
  if (lessons.length === 0) return <Text style={ui.body}>No lessons match this view.</Text>;
  return <View style={styles.list} role="list" accessibilityLabel="My lessons">
    {!compact && <View style={[styles.columns, styles.header]}>
      <Text style={[styles.when, styles.strong]}>When</Text>
      <Text style={[styles.person, styles.strong]}>{participantHeading}</Text>
      <Text style={[styles.headerTopic, styles.strong]}>What</Text>
    </View>}
    {lessons.map(lesson => {
      let participant = [lesson.tutorFirstName, lesson.tutorLastName].filter(Boolean).join(' ') || 'Tutor';
      if (tutorOnly) participant = lesson.studentDisplayName;
      let status = `${lesson.status} · ${lessonMinutes(lesson)} min`;
      if (lesson.status === 'Cancelled' && lesson.cancellationReason) {
        status = `Cancelled · ${lesson.cancellationReason}`;
      }
      const date = `${shortDate(lesson.startsAtUtc)} ${new Date(lesson.startsAtUtc).getFullYear()} · ${timeLabel(lesson.startsAtUtc)}–${timeLabel(lesson.endsAtUtc)}`;
      return <View key={lesson.lessonId} role="listitem" style={styles.card}>
        <View style={[styles.columns, compact && styles.stacked]}>
          <Text style={[styles.when, compact && styles.fullWidth]}>{date}</Text>
          <Text style={[styles.person, compact && styles.fullWidth]}>{participant}</Text>
          <Pressable accessibilityRole="button" accessibilityLabel={`Open ${lesson.subject} lesson with ${participant} on ${date}`}
            onPress={() => onOpen(lesson)} style={[styles.topic, compact && styles.fullWidth]}>
            <Text style={styles.strong}>{lesson.subject}</Text>
            <Text style={styles.small}>{lesson.plannedTopic || 'Topic not set'}</Text>
          </Pressable>
        </View>
        <View style={[styles.bottom, compact && styles.stacked]}>
          <View style={styles.state}>
            <Text style={styles.small}>{status}</Text>
            <Text style={styles.small}>{paymentLabel(lesson)}</Text>
          </View>
          <View style={ui.row}>
            <WorkspaceButton label="Materials" accessibilityLabel={`Materials for ${lesson.subject} on ${shortDate(lesson.startsAtUtc)}`}
              onPress={() => onOpen(lesson)} />
            {renderActions?.(lesson)}
            {lesson.status === 'Scheduled' && <WorkspaceButton label="Cancel" accessibilityLabel={`Cancel ${lesson.subject} on ${shortDate(lesson.startsAtUtc)}`}
              onPress={() => onCancel(lesson)} />}
          </View>
        </View>
      </View>;
    })}
  </View>;
}

export function LessonPagination({ data, onSelect }: { data: LessonPage; onSelect: (page: number) => void }) {
  const lastPage = Math.max(1, Math.ceil(data.totalCount / data.pageSize));
  let first = 0;
  if (data.totalCount > 0) first = (data.page - 1) * data.pageSize + 1;
  const last = Math.min(data.page * data.pageSize, data.totalCount);
  return <View role="navigation" accessibilityLabel="Lesson pages" style={ui.row}>
    <WorkspaceButton label="Previous" accessibilityLabel="Previous lesson page" disabled={data.page <= 1}
      onPress={() => onSelect(data.page - 1)} />
    <Text accessibilityLiveRegion="polite" style={ui.body}>{first}–{last} of {data.totalCount} lessons · Page {data.page} of {lastPage}</Text>
    <WorkspaceButton label="Next" accessibilityLabel="Next lesson page" disabled={data.page >= lastPage}
      onPress={() => onSelect(data.page + 1)} />
  </View>;
}

const styles = StyleSheet.create({
  list: { gap: 12 },
  columns: { flexDirection: 'row', alignItems: 'flex-start', gap: 12 },
  header: { backgroundColor: '#d9d9d9', paddingHorizontal: 16, paddingVertical: 12 },
  when: { ...ui.body, width: 240 },
  person: { ...ui.body, width: 200 },
  headerTopic: { flex: 1, minWidth: 0 },
  topic: { flex: 1, minWidth: 0, minHeight: 40, justifyContent: 'center' },
  strong: { ...ui.body, fontWeight: '600' },
  small: { ...ui.body, fontSize: 13, lineHeight: 19 },
  card: { backgroundColor: '#fff', borderColor: '#d1d1d1', borderWidth: 1, paddingHorizontal: 16, paddingVertical: 11, gap: 8 },
  bottom: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 12 },
  state: { flexShrink: 1, gap: 2 },
  stacked: { flexDirection: 'column', alignItems: 'stretch', gap: 8 },
  fullWidth: { width: '100%', flex: 0 },
});
