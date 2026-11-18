import { useCallback } from 'react';
import { useRouter } from 'expo-router';
import { Text, View } from 'react-native';
import type { ApiClient } from '@/shared/api/api-client';
import { WorkspaceButton, workspaceStyles as ui } from '@/shared/components/ui/workspace-controls';
import { useWorkspaceQuery } from '@/shared/models/use-workspace-query';
import { StudentPage, StudentQueryFeedback, studentStyles } from '../../workspace/components/student-workspace';
import { StudentTable } from '../../workspace/components/student-table';
import { AgreementPicker } from '../../workspace/components/agreement-picker';
import { useStudentAgreements } from '../../workspace/models/use-student-agreements';
import { studentLessonRoute } from '../../workspace/models/student-routes';
import { money, shortDate } from '@/features/tutor/workspace/models/workspace-format';
import { getStudentCharges, outstandingByCurrency } from '../api/charges';

export function StudentSettlementsScreen() {
  const router = useRouter();
  const agreements = useStudentAgreements();
  const agreementId = agreements.agreement?.agreementId;
  const load = useCallback(async (api: ApiClient, signal: AbortSignal) => {
    if (!agreementId) return [];
    return getStudentCharges(api, agreementId, signal);
  }, [agreementId]);
  const charges = useWorkspaceQuery(load);
  let outstanding = 'Nothing outstanding';
  if (charges.data) {
    outstanding = outstandingByCurrency(charges.data).map(([currency, amount]) => money(amount, currency)).join(' + ') || outstanding;
  }
  return <StudentPage code="S04" title="My settlements">
    <Text style={ui.body}>Review charges separately for each tutoring agreement.</Text>
    <StudentQueryFeedback {...agreements} />
    {agreements.data && <AgreementPicker agreements={agreements.data} selected={agreementId} onSelect={agreements.selectAgreement} />}
    {agreementId && <>
      <StudentQueryFeedback {...charges} />
      {charges.data && <>
        <View style={studentStyles.notice}><Text style={studentStyles.noticeText}>Outstanding: {outstanding}</Text></View>
        <StudentTable label="Lesson charges" headings={['Lesson', 'Amount', 'Payment status']}
          rows={charges.data.map(charge => {
            let status = 'Unpaid';
            if (charge.paidAtUtc) status = `Paid · ${shortDate(charge.paidAtUtc)}`;
            return { key: charge.chargeId, cells: [<WorkspaceButton textOnly key={charge.chargeId} label={shortDate(charge.startsAtUtc)}
              onPress={() => router.push(studentLessonRoute(charge.lessonId))} />, money(charge.amount, charge.currency), status] };
          })} />
        {charges.data.length === 0 && <Text style={ui.body}>No charges for this agreement.</Text>}
      </>}
    </>}
    <View style={studentStyles.notice}><Text style={studentStyles.noticeText}>Payment statuses are recorded by the tutor. Contact the tutor if a record needs correction.</Text></View>
  </StudentPage>;
}
