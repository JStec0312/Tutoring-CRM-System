import { useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useApi } from '@/shared/api/api-context';
import type { ApiClient } from '@/shared/api/api-client';
import { WorkspaceButton, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { chargeTotals, getLessonCharges, markChargeUnpaid, recordPayments, type LessonCharge } from '../api/lesson-charges';
import { money, shortDate } from '../../workspace/models/workspace-format';
import { useTutorQuery } from '../../workspace/models/use-tutor-query';
import { useTutorMutation } from '../../workspace/models/use-tutor-mutation';
import { WorkspaceMetrics, WorkspaceTable } from '../../workspace/components/workspace-data';
import { QueryFeedback, TutorPage } from '../../workspace/components/tutor-workspace';

const filters = ['Unpaid', 'Paid', 'All charges'] as const;

export function PaymentsScreen() {
  const router = useRouter();
  const api = useApi();
  const { agreementId, lessonId } = useLocalSearchParams<{ agreementId?: string; lessonId?: string }>();
  const load = useCallback((api: ApiClient, signal: AbortSignal) =>
    getLessonCharges(api, signal, { agreementId, lessonId }), [agreementId, lessonId]);
  const query = useTutorQuery(load);
  const mutation = useTutorMutation();
  const [status, setStatus] = useState<(typeof filters)[number]>('Unpaid');
  const [currency, setCurrency] = useState('');
  const [selected, setSelected] = useState<Set<string>>(() => new Set());
  const [feedback, setFeedback] = useState('');
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
  const selectedCharges = visible.filter(charge => !charge.paidAtUtc && selected.has(charge.chargeId));

  function clearSelection() {
    setSelected(new Set());
    setFeedback('');
    mutation.setError('');
  }

  function toggleSelection(id: string) {
    setSelected(current => {
      const next = new Set(current);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  function record(charges: LessonCharge[]) {
    setFeedback('');
    void mutation.run(async () => {
      await recordPayments(api, charges.map(charge => charge.chargeId));
      setSelected(current => {
        const next = new Set(current);
        for (const charge of charges) next.delete(charge.chargeId);
        return next;
      });
      setFeedback(charges.length === 1 ? 'Payment recorded.' : `${charges.length} charges marked as paid.`);
      query.reload();
    });
  }

  function undo(charge: LessonCharge) {
    setFeedback('');
    void mutation.run(async () => {
      await markChargeUnpaid(api, charge.chargeId);
      setFeedback('Charge marked as unpaid.');
      query.reload();
    });
  }

  return <TutorPage title="Payments">
    <QueryFeedback {...query} />
    {!!feedback && <Text accessibilityLiveRegion="polite" style={ui.body}>{feedback}</Text>}
    {!!mutation.error && <View style={ui.row}>
      <Text accessibilityRole="alert" style={ui.error}>{mutation.error}</Text>
      <WorkspaceButton label="Refresh payments" disabled={mutation.busy} onPress={() => {
        mutation.setError('');
        query.reload();
      }} />
    </View>}
    {query.data && <>
      <WorkspaceMetrics items={[
        { label: 'Outstanding', value: totalLabel(unpaid) },
        { label: 'Unpaid agreements', value: String(new Set(unpaid.map(charge => charge.agreementId)).size) },
        { label: 'Paid this month', value: totalLabel(paidThisMonth) },
      ]} />
      <View style={ui.row}>
        {filters.map(filter => <WorkspaceButton key={filter} label={filter} primary={status === filter} disabled={mutation.busy}
          onPress={() => { setStatus(filter); clearSelection(); }} />)}
      </View>
      {currencies.length > 1 && <View style={ui.row}>
        <Text style={ui.body}>Currency</Text>
        <WorkspaceButton label="All currencies" primary={!currency} disabled={mutation.busy}
          onPress={() => { setCurrency(''); clearSelection(); }} />
        {currencies.map(code => <WorkspaceButton key={code} label={code} primary={currency === code} disabled={mutation.busy}
          onPress={() => { setCurrency(code); clearSelection(); }} />)}
      </View>}
      {selectedCharges.length > 0 && <View style={ui.row}>
        <WorkspaceButton primary label={`Record selected (${selectedCharges.length})`} disabled={mutation.busy}
          onPress={() => record(selectedCharges)} />
        <WorkspaceButton label="Clear selection" disabled={mutation.busy} onPress={clearSelection} />
        <Text style={ui.body}>{totalLabel(selectedCharges)}</Text>
      </View>}
      {visible.length === 0 && <Text style={ui.body}>No charges match these filters.</Text>}
      <WorkspaceTable headers={['Agreement', 'Lesson date', 'Amount', 'Action']} rows={visible.map(charge => ({
        key: charge.chargeId,
        cells: [
          `${charge.studentName} · ${charge.subject}`, shortDate(charge.startsAtUtc),
          `${money(charge.amount, charge.currency)} · ${charge.paidAtUtc ? 'Paid' : 'Unpaid'}`,
          <View key="actions" style={styles.actions}>
            {charge.paidAtUtc ? <>
              <Text style={ui.body}>Paid {shortDate(charge.paidAtUtc)}</Text>
              <WorkspaceButton label="Mark as unpaid" disabled={mutation.busy} onPress={() => undo(charge)} />
            </> : <>
              <Pressable accessibilityRole="checkbox"
                accessibilityLabel={`Select payment for ${charge.studentName}, ${charge.subject}, ${shortDate(charge.startsAtUtc)}`}
                accessibilityState={{ checked: selected.has(charge.chargeId), disabled: mutation.busy }}
                disabled={mutation.busy} onPress={() => toggleSelection(charge.chargeId)} style={styles.selector}>
                <View style={[styles.checkbox, selected.has(charge.chargeId) && styles.checked]}>
                  {selected.has(charge.chargeId) && <Text style={styles.tick}>✓</Text>}
                </View>
                <Text style={ui.body}>Select</Text>
              </Pressable>
              <WorkspaceButton label="Record payment" disabled={mutation.busy} onPress={() => record([charge])} />
            </>}
          </View>,
        ],
      }))} />
      {(agreementId || lessonId) && <WorkspaceButton label="All payments" disabled={mutation.busy} onPress={() => {
        clearSelection();
        router.replace('/tutor/payments');
      }} />}
    </>}
  </TutorPage>;
}

const styles = StyleSheet.create({
  actions: { gap: 4 },
  selector: { minHeight: 44, flexDirection: 'row', alignItems: 'center', gap: 8 },
  checkbox: { width: 20, height: 20, borderRadius: 4, borderWidth: 1, borderColor: '#666',
    alignItems: 'center', justifyContent: 'center' },
  checked: { backgroundColor: '#292929', borderColor: '#292929' },
  tick: { color: '#fff', fontSize: 14 },
});

function totalLabel(charges: LessonCharge[]) {
  return chargeTotals(charges).map(([currency, total]) => money(total, currency)).join(' + ') || '0';
}
