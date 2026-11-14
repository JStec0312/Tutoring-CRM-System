import { useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useRef, useState } from 'react';
import { Text, View } from 'react-native';
import { useApi } from '@/shared/api/api-context';
import type { ApiClient } from '@/shared/api/api-client';
import { WorkspaceButton, WorkspaceCard, WorkspaceField, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { getLessonCharges, markChargePaid } from '../api/lesson-charges';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';
import { dateKey, lessonMinutes, money, parseLocalDateTime, shortDate } from '../../workspace/models/workspace-format';
import { errorMessage, useTutorQuery } from '../../workspace/models/use-tutor-query';

export function RecordPaymentScreen() {
  const { chargeId, agreementId, lessonId } = useLocalSearchParams<{ chargeId: string; agreementId?: string; lessonId?: string }>();
  const router = useRouter();
  const api = useApi();
  const load = useCallback((client: ApiClient, signal: AbortSignal) =>
    getLessonCharges(client, signal, { agreementId }), [agreementId]);
  const query = useTutorQuery(load);
  const charge = query.data?.find(item => item.chargeId === chargeId);
  const [date, setDate] = useState(dateKey(new Date()));
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);
  const [recorded, setRecorded] = useState(false);
  const inFlight = useRef(false);
  const back = () => {
    if (router.canGoBack()) router.back();
    else router.replace({ pathname: '/tutor/payments', params: { agreementId, lessonId } });
  };
  const remaining = charge ? query.data?.filter(item => item.chargeId !== chargeId && !item.paidAtUtc &&
    item.agreementId === charge.agreementId && item.currency === charge.currency).reduce((total, item) => total + item.amount, 0) : undefined;

  async function confirm() {
    if (inFlight.current) return;
    const paymentDate = parseLocalDateTime(date.trim(), '00:00');
    if (!paymentDate || paymentDate > new Date()) {
      setError('Enter a valid payment date up to today.');
      return;
    }
    inFlight.current = true;
    setSaving(true);
    setError('');
    try {
      await markChargePaid(api, chargeId, paymentDate);
      setRecorded(true);
      query.reload();
    } catch (error) {
      setError(errorMessage(error));
    } finally {
      inFlight.current = false;
      setSaving(false);
    }
  }

  const paid = recorded || Boolean(charge?.paidAtUtc);
  return <TutorPage title={recorded ? 'Payment recorded' : 'Record a payment'}>
    <QueryFeedback {...query} />
    {query.data && !charge && <Text style={ui.body}>This charge is no longer available.</Text>}
    {charge && <>
      <WorkspaceCard title={`${charge.studentName} · ${charge.subject}`}
        detail={`${shortDate(charge.startsAtUtc)} · ${lessonMinutes(charge)} minutes · ${money(charge.amount, charge.currency)}${paid ? ' · Paid' : ''}`} />
      {!paid && <WorkspaceField label="Payment date (YYYY-MM-DD)" value={date} onChangeText={setDate} editable={!saving} />}
      {remaining !== undefined && <Text style={ui.body}>
        {paid ? 'Remaining balance' : 'Balance after confirmation'} · {money(remaining, charge.currency)}
      </Text>}
    </>}
    {!!error && <Text accessibilityRole="alert" style={ui.error}>{error}</Text>}
    <View style={ui.row}>
      {charge && !paid && <WorkspaceButton primary label={saving ? 'Saving…' : 'Mark as paid'} disabled={saving} onPress={() => void confirm()} />}
      <WorkspaceButton label={paid ? 'Back to payments' : 'Cancel'} disabled={saving} onPress={back} />
    </View>
  </TutorPage>;
}
