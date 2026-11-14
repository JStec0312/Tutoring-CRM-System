import { StyleSheet, Text, View } from 'react-native';
import type { CalendarLesson, DashboardData } from './dashboard-api';

export function SummaryCards({ data }: { data: DashboardData }) {
  const hours = new Intl.NumberFormat('en-GB', { maximumFractionDigits: 1 }).format(data.teachingSeconds / 3600);
  const metrics = [{ label: 'Completed lessons', value: String(data.completed) }, { label: 'Teaching time', value: `${hours} hours` }, { label: 'Unpaid lessons', value: String(data.unpaidLessons) }];
  return <View style={styles.cards}>{metrics.map(item => <View key={item.label} style={styles.card}><Text style={styles.label}>{item.label}</Text><Text style={styles.value}>{item.value}</Text></View>)}</View>;
}

export function LessonList({ lessons, compact }: { lessons: CalendarLesson[]; compact: boolean }) {
  const time = (value: string) => new Date(value).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' });
  return <View>
    <Text style={styles.heading}>Upcoming lessons today</Text>
    {lessons.length === 0 && <Text style={styles.empty}>No upcoming lessons today.</Text>}
    {!compact && lessons.length > 0 && <View style={[styles.row, styles.tableHeader]}><Text style={styles.cell}>Time</Text><Text style={styles.cell}>Student / subject</Text><Text style={styles.cell}>Status</Text></View>}
    {lessons.map(lesson => compact ? <View key={lesson.lessonId} style={styles.mobileRow}><Text style={styles.rowText}>{time(lesson.startsAtUtc)}–{time(lesson.endsAtUtc)}</Text><Text style={styles.rowText}>{lesson.studentDisplayName} / {lesson.subject}</Text><Text style={styles.muted}>{lesson.status}</Text></View> : <View key={lesson.lessonId} style={styles.row}><Text style={styles.cell}>{time(lesson.startsAtUtc)}–{time(lesson.endsAtUtc)}</Text><Text style={styles.cell}>{lesson.studentDisplayName} / {lesson.subject}</Text><Text style={styles.cell}>{lesson.status}</Text></View>)}
  </View>;
}

export function AttentionNotice({ count }: { count: number }) {
  return <View style={styles.notice}><Text style={styles.muted}>{count === 0 ? 'No agreements with unpaid lessons.' : `Needs attention: ${count} ${count === 1 ? 'agreement has' : 'agreements have'} unpaid lessons.`}</Text></View>;
}

const styles = StyleSheet.create({
  cards: { flexDirection: 'row', flexWrap: 'wrap', gap: 12 }, card: { flexGrow: 1, flexBasis: 150, borderColor: '#dedede', borderWidth: 1, borderRadius: 3, padding: 16, gap: 10 },
  label: { color: '#333', fontSize: 12, fontWeight: '600' }, value: { color: '#222', fontSize: 21, fontWeight: '700' },
  heading: { color: '#222', fontSize: 18, fontWeight: '700', marginBottom: 14 }, row: { flexDirection: 'row', borderWidth: 1, borderColor: '#e5e5e5', padding: 12 },
  tableHeader: { backgroundColor: '#ddd' }, cell: { flex: 1, color: '#333', fontSize: 12 }, mobileRow: { borderWidth: 1, borderColor: '#e5e5e5', padding: 12, gap: 5 },
  rowText: { color: '#333', fontSize: 13 }, muted: { color: '#777', fontSize: 12 }, empty: { color: '#555', fontSize: 13 }, notice: { padding: 14, backgroundColor: '#dedede' },
});
