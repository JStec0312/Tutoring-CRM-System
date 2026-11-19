import { useCallback } from 'react';
import { useLocalSearchParams, useRouter } from 'expo-router';
import type { ApiClient } from '@/shared/api/api-client';
import { useWorkspaceQuery } from '@/shared/models/use-workspace-query';
import { getLessonPage, type LessonView } from '../api/get-lesson-page';

export function useLessonPage(tutorOnly: boolean) {
  const params = useLocalSearchParams<{ page?: string; view?: string; agreementId?: string }>();
  const router = useRouter();
  let page = Number(params.page ?? '1');
  if (!Number.isSafeInteger(page) || page < 1 || page > 2_147_483_647) page = 1;
  let view: LessonView = 'all';
  if (params.view === 'upcoming' || params.view === 'past') view = params.view;
  const agreementId = params.agreementId;
  const load = useCallback((api: ApiClient, signal: AbortSignal) =>
    getLessonPage(api, { tutorOnly, page, view, agreementId }, signal), [tutorOnly, page, view, agreementId]);
  const query = useWorkspaceQuery(load);

  function selectView(next: LessonView) { router.setParams({ page: '1', view: next }); }
  function selectPage(next: number) { router.setParams({ page: String(next) }); }
  function clearAgreement() { router.setParams({ agreementId: '', page: '1' }); }
  return { ...query, view, agreementId, selectView, selectPage, clearAgreement };
}
