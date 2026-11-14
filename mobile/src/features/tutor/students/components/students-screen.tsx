import { useRouter } from 'expo-router';
import { useState } from 'react';
import { Pressable, Text, useWindowDimensions, View } from 'react-native';
import type { ApiClient } from '@/shared/api/api-client';
import { WorkspaceButton, WorkspaceCard, WorkspaceColumns, WorkspaceField, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { getPendingInvitations, getStudents } from '../api/get-students';
import { getOverdueAgreements } from '../../payments/api/get-overdue-agreements';
import { quantity, unpaidLabel } from '../../workspace/models/workspace-format';
import { studentRoute } from '../../workspace/models/tutor-routes';
import { useTutorQuery } from '../../workspace/models/use-tutor-query';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';

const filters = ['Active', 'All', 'Inactive'] as const;

async function loadStudents(api: ApiClient, signal: AbortSignal) {
  const [students, invitations, overdue] = await Promise.all([
    getStudents(api, signal),
    getPendingInvitations(api, signal),
    getOverdueAgreements(api, signal),
  ]);
  return { students, invitations, overdue };
}

export function StudentsScreen() {
  const router = useRouter();
  const compact = useWindowDimensions().width < 900;
  const [search, setSearch] = useState('');
  const [filter, setFilter] = useState<(typeof filters)[number]>('All');
  const query = useTutorQuery(loadStudents);
  const needle = search.trim().toLowerCase();
  const students = query.data?.students
    .filter(student => [student.displayName, student.email].some(value => value?.toLowerCase().includes(needle)))
    .filter(student => {
      const active = student.agreements.some(agreement => agreement.status === 'Active');
      return filter === 'All' || active === (filter === 'Active');
    })
    .sort((a, b) => a.displayName.localeCompare(b.displayName));

  return <TutorPage title="Students" subtitle="One student profile, with separate agreements for each subject.">
    <QueryFeedback {...query} />
    <WorkspaceColumns compact={compact}>
      <View style={compact ? ui.stack : ui.main}>
        <WorkspaceField label="Search students" placeholder="Name or email" value={search} onChangeText={setSearch} />
        <View style={ui.row}>
          {filters.map((item, index) => <View key={item} style={ui.row}>
            {index > 0 && <Text style={ui.body}>/</Text>}
            <Pressable accessibilityRole="button" accessibilityState={{ selected: item === filter }}
              hitSlop={6} onPress={() => setFilter(item)}>
              <Text style={[ui.body, item === filter && { fontWeight: '700', textDecorationLine: 'underline' }]}>{item}</Text>
            </Pressable>
          </View>)}
        </View>
        {students?.length === 0 && <Text style={ui.body}>No students match this view.</Text>}
        {students?.map(student => {
          const active = student.agreements.filter(agreement => agreement.status === 'Active').length;
          const inactive = student.agreements.length - active;
          const subjects = [...new Set(student.agreements.map(agreement => agreement.subject))].join(', ');
          const statuses = [active && quantity(active, 'active agreement'), inactive && quantity(inactive, 'inactive agreement')].filter(Boolean);
          const unpaid = unpaidLabel(query.data?.overdue.filter(agreement => agreement.studentId === student.studentId) ?? []);
          return <WorkspaceCard key={student.studentId} title={student.displayName}
            detail={[`${subjects} · ${statuses.join(' · ')}`, unpaid && `${unpaid} unpaid`].filter(Boolean).join('\n')}
            onPress={() => router.push(studentRoute(student.studentId))} />;
        })}
      </View>
      <View style={compact ? ui.stack : ui.aside}>
        <WorkspaceButton primary label="Add student" onPress={() => router.push('/tutor/students/add')} />
        <WorkspaceButton label="Invite by email" onPress={() => router.push({ pathname: '/tutor/students/add', params: { mode: 'invite' } })} />
        {query.data && <WorkspaceCard title="Pending invitations"
          detail={query.data.invitations.map(invite => `${invite.email} · ${invite.subject} · Awaiting acceptance`).join('\n') || 'No pending invitations.'} />}
      </View>
    </WorkspaceColumns>
  </TutorPage>;
}
