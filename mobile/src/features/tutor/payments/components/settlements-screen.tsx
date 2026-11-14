import { useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useState } from 'react';
import { Text, View } from 'react-native';
import type { ApiClient } from '@/shared/api/api-client';
import { WorkspaceButton, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { chargeTotals, getLessonCharges, type LessonCharge } from '../api/lesson-charges';
import { money, shortDate } from '../../workspace/models/workspace-format';
import { useTutorQuery } from '../../workspace/models/use-tutor-query';
import { WorkspaceMetrics, WorkspaceTable } from '../../workspace/components/workspace-data';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';

const filters = ['Unpaid', 'Paid', 'All charges'] as const;

export function PaymentsScreen() {
  const router = useRouter();
  const { agreementId, lessonId } = useLocalSearchParams<{ agreementId?: string; lessonId?: string }>();
  const load = useCallback((api: ApiClient, signal: AbortSignal) =>
    getLessonCharges(api, signal, { agreementId, lessonId }), [agreementId, lessonId]);
  const query = useTutorQuery(load);
  const [status, setStatus] = useState<(typeof filters)[number]>('Unpaid');
  const [currency, setCurrency] = useState('');
  const charges = query.data ?? [];
  const currencies = [...new Set(charges.map(charge => charge.currency))].sort();
  const byCurrency = charges.filter(charge => !currency || charge.currency === currency);
  const unpaid = byCurrency.filter(charge => !charge.paidAtUtc);
  const today = new Date();
  const monthStart = new Date(today.getFullYear(), today.getMonth(), 1);
  const monthEnd = new Date(today.getFullYear(), today.getMonth() + 1, 1);
  const paidThisMonth = byCurrency.filter(charge => charge.paidAtUtc &&
    Date.parse(charge.paidAtUtc) >= monthStart.getTime() && Date.parse(charge.paidAtUtc) < monthEnd.getTime());
  const visible = byCurrency.filter(charge => status === 'All charges' || Boolean(charge.paidAtUtc) === (status === 'Paid'));

  return <TutorPage title="Payments">
    <QueryFeedback {...query} />
    {query.data && <>
      <WorkspaceMetrics items={[
        { label: 'Outstanding', value: totalLabel(unpaid) },
        { label: 'Unpaid agreements', value: String(new Set(unpaid.map(charge => charge.agreementId)).size) },
        { label: 'Paid this month', value: totalLabel(paidThisMonth) },
      ]} />
      <View style={ui.row}>
        {filters.map(filter => <WorkspaceButton key={filter} label={filter} primary={status === filter} onPress={() => setStatus(filter)} />)}
      </View>
      {currencies.length > 1 && <View style={ui.row}>
        <Text style={ui.body}>Currency</Text>
        <WorkspaceButton label="All currencies" primary={!currency} onPress={() => setCurrency('')} />
        {currencies.map(code => <WorkspaceButton key={code} label={code} primary={currency === code} onPress={() => setCurrency(code)} />)}
      </View>}
      {visible.length === 0 && <Text style={ui.body}>No charges match these filters.</Text>}
      <WorkspaceTable headers={['Agreement', 'Lesson date', 'Amount', 'Action']} rows={visible.map(charge => ({
        key: charge.chargeId,
        cells: [
          `${charge.studentName} · ${charge.subject}`, shortDate(charge.startsAtUtc),
          `${money(charge.amount, charge.currency)} · ${charge.paidAtUtc ? 'Paid' : 'Unpaid'}`,
          charge.paidAtUtc ? `Paid ${shortDate(charge.paidAtUtc)}` : <WorkspaceButton label="Record payment"
            onPress={() => router.push({ pathname: '/tutor/payments/[chargeId]', params: { chargeId: charge.chargeId, agreementId: charge.agreementId, lessonId } })} />,
        ],
      }))} />
      {(agreementId || lessonId) && <WorkspaceButton label="All payments" onPress={() => router.replace('/tutor/payments')} />}
    </>}
  </TutorPage>;
}

function totalLabel(charges: LessonCharge[]) {
  return chargeTotals(charges).map(([currency, total]) => money(total, currency)).join(' + ') || '0';
}
