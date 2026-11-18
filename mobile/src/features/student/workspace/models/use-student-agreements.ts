import { useState } from 'react';
import { useWorkspaceQuery } from '@/shared/models/use-workspace-query';
import { getStudentAgreements } from '../../tutors/api/agreements';

export function useStudentAgreements(initialId?: string) {
  const query = useWorkspaceQuery(getStudentAgreements);
  const [selectedId, setSelectedId] = useState(initialId);
  let agreement = query.data?.find(item => item.agreementId === selectedId);
  if (!selectedId) agreement = query.data?.[0];
  return { ...query, agreement, selectAgreement: setSelectedId };
}
