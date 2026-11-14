import { useRouter } from 'expo-router';
import { Text, useWindowDimensions, View } from 'react-native';
import { WorkspaceButton, WorkspaceCard, WorkspaceColumns, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { loadToday } from '../api/load-today';
import { lessonSummary, lessonTime, quantity, unpaidLabel } from '../../workspace/models/workspace-format';
import { lessonRoute } from '../../workspace/models/tutor-routes';
import { useTutorQuery } from '../../workspace/models/use-tutor-query';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';

export function TodayScreen() {
  const router = useRouter();
  const compact = useWindowDimensions().width < 900;
  const query = useTutorQuery(loadToday);
  const date = new Intl.DateTimeFormat('en-GB', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }).format(new Date());
  const data = query.data;
  const unpaid = data ? unpaidLabel(data.overdue) : '';

  return <TutorPage title="Today" subtitle={date}>
    <QueryFeedback {...query} />
    <WorkspaceColumns compact={compact}>
      <View style={compact ? ui.stack : ui.main}>
        <Text accessibilityRole="header" style={ui.sectionHeading}>Upcoming lessons</Text>
        {data?.upcoming.length === 0 && <Text style={ui.body}>No upcoming lessons today.</Text>}
        {data?.upcoming.map(lesson => <WorkspaceCard key={lesson.lessonId}
          title={`${lessonTime(lesson)} · ${lesson.studentDisplayName}`}
          detail={lessonSummary(lesson)} onPress={() => router.push(lessonRoute(lesson))} />)}
        <WorkspaceButton primary label="Schedule lesson" onPress={() => router.push('/tutor/schedule')} />
      </View>
      <View style={compact ? ui.stack : ui.aside}>
        <Text accessibilityRole="header" style={ui.sectionHeading}>Needs attention</Text>
        {data && <>
          <WorkspaceCard title={`${quantity(data.toClose.length, 'lesson')} to close`}
            detail={data.toClose.length ? 'Review outcomes and notes' : 'All past lessons have an outcome.'}
            onPress={data.toClose.length ? () => router.push('/tutor/notifications') : undefined} />
          <WorkspaceCard title={unpaid ? `${unpaid} unpaid` : data.overdue.length ? 'Unpaid lessons' : 'No unpaid lessons'}
            detail={quantity(data.overdue.length, 'tutoring agreement')}
            onPress={data.overdue.length ? () => router.push('/tutor/payments') : undefined} />
        </>}
        <WorkspaceButton label="Add student" onPress={() => router.push('/tutor/students/add')} />
        <WorkspaceButton label="View statistics" onPress={() => router.push('/tutor/statistics')} />
      </View>
    </WorkspaceColumns>
  </TutorPage>;
}
