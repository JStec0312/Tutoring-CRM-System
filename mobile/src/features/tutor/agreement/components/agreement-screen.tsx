import { useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useRef, useState } from 'react';
import { Text, useWindowDimensions, View } from 'react-native';
import { useApi } from '@/shared/api/api-context';
import { WorkspaceButton, WorkspaceCard, WorkspaceColumns, WorkspaceField, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { getAgreementOverview, getStudentHistory, updateAgreement, type AgreementOverview } from '../api/agreement';
import { getOverdueAgreements } from '../../payments/api/get-overdue-agreements';
import { estimateLabel, lessonMinutes, money, parseRate, quantity, shortDate, timeLabel, unpaidLabel } from '../../workspace/models/workspace-format';
import { errorMessage, useTutorQuery } from '../../workspace/models/use-tutor-query';
import { lessonRoute, studentRoute } from '../../workspace/models/tutor-routes';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';
import type { ApiClient } from '@/shared/api/api-client';

export function AgreementScreen() {
  const { agreementId } = useLocalSearchParams<{ agreementId: string }>();
  const router = useRouter();
  const compact = useWindowDimensions().width < 900;
  const [panel, setPanel] = useState<'history' | 'settings'>();
  const load = useCallback(async (api: ApiClient, signal: AbortSignal) => {
    const [agreement, overdue] = await Promise.all([
      getAgreementOverview(api, agreementId, signal),
      getOverdueAgreements(api, signal),
    ]);
    const history = await getStudentHistory(api, agreement.studentId, signal);
    return {
      agreement,
      history: history.filter(lesson => lesson.tutoringAgreementId === agreementId),
      overdue: overdue.filter(item => item.tutoringAgreementId === agreementId),
    };
  }, [agreementId]);
  const query = useTutorQuery(load);
  const data = query.data;
  if (!data) return <TutorPage title="Tutoring agreement">
    <QueryFeedback {...query} />
    <WorkspaceButton label="Back to students" onPress={() => router.navigate('/tutor/students')} />
  </TutorPage>;

  const { agreement, history, overdue } = data;
  const { nextLesson, materialCount } = agreement;
  const recent = history.find(lesson => lesson.status !== 'Scheduled');
  const rate = agreement.hourlyRate != null && agreement.currency
    ? `${money(agreement.hourlyRate, agreement.currency)}/hour` : 'No hourly rate';
  const unpaid = unpaidLabel(overdue);
  return <TutorPage title={`${agreement.subject} · ${agreement.studentDisplayName}`} subtitle={`${agreement.status} agreement · ${rate}`}>
    <WorkspaceColumns compact={compact}>
      <View style={compact ? ui.stack : ui.main}>
        {nextLesson
          ? <WorkspaceCard title={`Next lesson · ${shortDate(nextLesson.startsAtUtc)}, ${timeLabel(nextLesson.startsAtUtc)}`}
              detail={[`${lessonMinutes(nextLesson)} minutes`, nextLesson.status, estimateLabel(nextLesson)].filter(Boolean).join(' · ')}
              onPress={() => router.push(lessonRoute(nextLesson))} />
          : <WorkspaceCard title="Next lesson" detail="No lesson scheduled." />}
        <WorkspaceButton primary disabled={agreement.status !== 'Active'} label="Schedule lesson"
          onPress={() => router.push({ pathname: '/tutor/schedule', params: { agreementId } })} />
        <Text accessibilityRole="header" style={ui.sectionHeading}>Recent lessons</Text>
        {recent
          ? <WorkspaceCard title={`${shortDate(recent.startsAtUtc)} · ${recent.topic || recent.subject}`}
              detail={`${recent.status} · ${lessonMinutes(recent)} minutes`} onPress={() => router.push(lessonRoute(recent))} />
          : <Text style={ui.body}>No past lessons yet.</Text>}
        <WorkspaceButton label="Lesson history & statistics" onPress={() => setPanel(panel === 'history' ? undefined : 'history')} />
        {panel === 'history' && <View style={ui.stack}>
          <WorkspaceCard title="Agreement statistics"
            detail={`${quantity(history.filter(lesson => lesson.status === 'Completed').length, 'completed lesson')} · ${quantity(
              history.filter(lesson => lesson.status === 'Missed').length, 'missed lesson')}`} />
          {history.map(lesson => <WorkspaceCard key={lesson.lessonId}
            title={`${shortDate(lesson.startsAtUtc)} · ${lesson.topic || lesson.subject}`}
            detail={`${lesson.status} · ${lessonMinutes(lesson)} minutes`} onPress={() => router.push(lessonRoute(lesson))} />)}
        </View>}
      </View>
      <View style={compact ? ui.stack : ui.aside}>
        <WorkspaceCard title="Payments"
          detail={unpaid ? `${unpaid} unpaid · ${quantity(overdue[0].unpaidLessonCount, 'charge')}` : 'No unpaid charges.'}
          onPress={() => router.push({ pathname: '/tutor/payments', params: { agreementId } })} />
        <WorkspaceCard title="Materials" detail={`${quantity(materialCount, 'file')} shared within this agreement`}
          onPress={() => router.push({ pathname: '/tutor/materials', params: { agreementId } })} />
        <WorkspaceButton label="Agreement settings" disabled={agreement.status === 'Ended'}
          onPress={() => setPanel(panel === 'settings' ? undefined : 'settings')} />
        {panel === 'settings' && <AgreementSettings agreement={agreement} agreementId={agreementId} reload={query.reload} />}
        {agreement.status === 'Suspended' && panel !== 'settings' &&
          <WorkspaceButton label="Reactivate agreement" onPress={() => setPanel('settings')} />}
        <WorkspaceButton label="Back to student profile" onPress={() => router.navigate(studentRoute(agreement.studentId))} />
      </View>
    </WorkspaceColumns>
  </TutorPage>;
}

function AgreementSettings({ agreement, agreementId, reload }: { agreement: AgreementOverview; agreementId: string; reload: () => void }) {
  const api = useApi();
  const [subject, setSubject] = useState(agreement.subject);
  const [notes, setNotes] = useState(agreement.notes ?? '');
  const [rate, setRate] = useState(String(agreement.hourlyRate ?? ''));
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [confirmDeactivate, setConfirmDeactivate] = useState(false);
  const inFlight = useRef(false);
  async function save(property: 'details' | 'hourly-rate' | 'status', body: object) {
    if (inFlight.current) return;
    inFlight.current = true;
    setBusy(true);
    setError('');
    try {
      await updateAgreement(api, agreementId, property, body);
      setConfirmDeactivate(false);
      reload();
    } catch (error) {
      setError(errorMessage(error));
    } finally {
      inFlight.current = false;
      setBusy(false);
    }
  }
  return <View style={ui.stack}>
    <WorkspaceField label="Subject" value={subject} onChangeText={setSubject} maxLength={100} editable={!busy} />
    <WorkspaceField label="Private notes" value={notes} onChangeText={setNotes} multiline maxLength={2000} editable={!busy} />
    <WorkspaceButton label="Save details" disabled={busy || !subject.trim()} onPress={() => void save('details', { subject: subject.trim(), notes })} />
    <WorkspaceField label="Hourly rate (PLN)" value={rate} onChangeText={setRate} keyboardType="decimal-pad" editable={!busy} />
    <WorkspaceButton label="Save rate" disabled={busy || Number.isNaN(parseRate(rate))} onPress={() => void save('hourly-rate', { hourlyRate: parseRate(rate) })} />
    {confirmDeactivate ? <View style={ui.stack}>
      <Text style={ui.sectionHeading}>Deactivate agreement</Text>
      <Text style={ui.body}>Review future lessons before continuing. Existing history and payments remain available.</Text>
      <WorkspaceButton primary label="Confirm deactivation" disabled={busy} onPress={() => void save('status', { isActive: false })} />
      <WorkspaceButton label="Keep active" disabled={busy} onPress={() => setConfirmDeactivate(false)} />
    </View> : <WorkspaceButton label={agreement.status === 'Active' ? 'Deactivate agreement' : 'Reactivate agreement'} disabled={busy}
      onPress={() => {
        if (agreement.status === 'Active') setConfirmDeactivate(true);
        else void save('status', { isActive: true });
      }} />}
    {!!error && <Text accessibilityRole="alert" style={ui.error}>{error}</Text>}
  </View>;
}
