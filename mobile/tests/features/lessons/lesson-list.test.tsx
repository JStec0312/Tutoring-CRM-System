// @vitest-environment jsdom
import { useEffect, useState } from 'react';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import type { ApiClient } from '@/shared/api/api-client';
import { ApiProvider } from '@/shared/api/api-context';
import { ApiError } from '@/shared/api/api-error';
import type { LessonListItem } from '@/shared/lessons/api/get-lesson-page';

const navigation = vi.hoisted(() => ({ push: vi.fn(), setParams: vi.fn(), initial: {} as Record<string, string> }));
vi.mock('react-native', () => vi.importActual('react-native-web'));
vi.mock('react-native-safe-area-context', async () => {
  const { View } = await vi.importActual<typeof import('react-native')>('react-native-web');
  return { SafeAreaView: View };
});
vi.mock('expo-router', () => ({
  useRouter: () => ({ push: navigation.push, setParams: navigation.setParams }),
  useLocalSearchParams: () => {
    const [params, setParams] = useState(navigation.initial);
    navigation.setParams.mockImplementation((patch: Record<string, string>) => setParams(current => ({ ...current, ...patch })));
    return params;
  },
  useFocusEffect: (effect: Parameters<typeof useEffect>[0]) => useEffect(effect, [effect]),
}));
vi.mock('@/features/tutor/materials/models/save-material-download', () => ({ saveMaterialDownload: vi.fn() }));

import { StudentLessonsScreen } from '@/features/student/lessons/components/lessons-screen';
import { TutorLessonsScreen } from '@/features/tutor/lessons/components/lessons-screen';
import { MaterialList } from '@/features/tutor/materials/components/materials-screen';
import { paymentLabel } from '@/shared/lessons/components/lesson-list';

const scheduled: LessonListItem = {
  lessonId: 'scheduled', tutoringAgreementId: 'math', subject: 'Mathematics', plannedTopic: 'Quadratic equations',
  startsAtUtc: '2026-11-20T14:00:00Z', endsAtUtc: '2026-11-20T15:00:00Z', status: 'Scheduled',
  cancellationReason: null, studentDisplayName: 'Anna Nowak', tutorFirstName: 'Maria', tutorLastName: 'Zielinska', charges: [],
};
const unpaid: LessonListItem = { ...scheduled, lessonId: 'unpaid', status: 'Completed', subject: 'Physics',
  charges: [{ chargeId: 'charge-1', amount: 90, currency: 'PLN', paidAtUtc: null }] };
const paid: LessonListItem = { ...scheduled, lessonId: 'paid', status: 'Completed',
  charges: [{ chargeId: 'charge-2', amount: 80, currency: 'PLN', paidAtUtc: '2026-11-18T12:00:00Z' }] };
const cancelled = { ...scheduled, lessonId: 'cancelled', status: 'Cancelled', cancellationReason: 'School trip' };

beforeEach(() => { vi.clearAllMocks(); vi.stubEnv('EXPO_PUBLIC_LESSONS_PAGE_SIZE', '4'); navigation.initial = {}; });
afterEach(() => { cleanup(); vi.unstubAllEnvs(); });

function setup(tutor: boolean, post = vi.fn().mockResolvedValue(undefined)) {
  let recorded = false;
  const get = vi.fn(async (path: string) => {
    const params = new URL(path, 'https://example.com').searchParams;
    expect(params.get('tutorOnly')).toBe(String(tutor));
    const page = Number(params.get('page'));
    let items = [scheduled, unpaid, paid, cancelled];
    if (recorded) items = [scheduled, { ...unpaid, charges: [{ ...unpaid.charges[0], paidAtUtc: '2026-11-19T10:00:00Z' }] }, paid, cancelled];
    if (params.get('view') === 'upcoming') return { items: [scheduled], page: 1, pageSize: 4, totalCount: 1 };
    if (page === 2) return { items: [paid, cancelled], page: 2, pageSize: 4, totalCount: 6 };
    return { items, page: 1, pageSize: 4, totalCount: 6 };
  });
  const send = vi.fn(async (path: string, body: unknown) => {
    await post(path, body);
    recorded = true;
  });
  const api = { get, post: send } as unknown as ApiClient;
  render(<ApiProvider client={api}>{tutor ? <TutorLessonsScreen /> : <StudentLessonsScreen />}</ApiProvider>);
  return { get, post: send };
}

it('shows payment status to the student without any payment mutation action', async () => {
  const { post } = setup(false);
  await screen.findByText('90 PLN · Unpaid');
  expect(screen.queryByRole('button', { name: /Mark .* as paid/ })).toBeNull();
  expect(screen.getAllByRole('button', { name: /^Cancel / })).toHaveLength(1);
  fireEvent.click(screen.getAllByRole('button', { name: /^Materials for/ })[0]);
  expect(navigation.push).toHaveBeenCalledWith({ pathname: '/student/lessons/[lessonId]', params: { lessonId: 'scheduled' } });
  expect(post).not.toHaveBeenCalled();
});

it('paginates on the API and resets to page one when the view changes', async () => {
  const { get } = setup(false);
  await screen.findByText('1–4 of 6 lessons · Page 1 of 2');
  expect(screen.getByRole('button', { name: 'Previous lesson page' }).getAttribute('aria-disabled')).toBe('true');
  fireEvent.click(screen.getByRole('button', { name: 'Next lesson page' }));
  await screen.findByText('5–6 of 6 lessons · Page 2 of 2');
  expect(screen.getByRole('button', { name: 'Next lesson page' }).getAttribute('aria-disabled')).toBe('true');
  fireEvent.click(screen.getByRole('button', { name: 'Upcoming' }));
  await screen.findByText('1–1 of 1 lessons · Page 1 of 1');
  expect(get.mock.calls.some(([path]) => path.includes('page=2'))).toBe(true);
  expect(navigation.setParams).toHaveBeenLastCalledWith({ page: '1', view: 'upcoming' });
});

it('records only unpaid charge IDs for the tutor and reloads the payment status', async () => {
  const { post } = setup(true);
  fireEvent.click(await screen.findByRole('button', { name: 'Mark Physics as paid' }));
  await waitFor(() => expect(post).toHaveBeenCalledWith('/api/tutors/me/lesson-charges/mark-paid', {
    lessonChargeIds: ['charge-1'], groupByAgreementAndCurrency: true,
  }));
  await screen.findByText('90 PLN · Paid');
  expect(screen.queryByRole('button', { name: 'Mark Physics as paid' })).toBeNull();
});

it('keeps the unpaid state and reports a rejected tutor payment', async () => {
  setup(true, vi.fn().mockRejectedValue(new ApiError(409, 'Payments.Changed', { detail: 'The charge has changed.' })));
  fireEvent.click(await screen.findByRole('button', { name: 'Mark Physics as paid' }));
  await screen.findByText('The charge has changed.');
  expect(screen.getByText('90 PLN · Unpaid')).toBeTruthy();
  expect(screen.getByRole('button', { name: 'Mark Physics as paid' }).getAttribute('aria-disabled')).not.toBe('true');
});

it('shows only the current lesson and unattached agreement materials in lesson context', async () => {
  const metadata = { fileName: 'notes.pdf', contentType: 'application/pdf', sizeBytes: 100, uploadedByTutor: true };
  const get = vi.fn().mockResolvedValue([
    { ...metadata, materialId: 'one', title: 'This lesson', lessonId: 'scheduled' },
    { ...metadata, materialId: 'two', title: 'Unassigned material', lessonId: null },
    { ...metadata, materialId: 'three', title: 'Different lesson', lessonId: 'other' },
  ]);
  render(<ApiProvider client={{ get } as unknown as ApiClient}><MaterialList agreementId="math" lessonId="scheduled" lessonContext /></ApiProvider>);
  await screen.findByText('This lesson');
  expect(screen.getByText('Unassigned material')).toBeTruthy();
  expect(screen.queryByText('Different lesson')).toBeNull();
  expect(screen.getAllByRole('button', { name: 'Attach' })).toHaveLength(1);
});

it('keeps no-charge, unpaid and paid labels distinct', () => {
  expect(paymentLabel(scheduled)).toBe('Not charged');
  expect(paymentLabel(unpaid)).toBe('90 PLN · Unpaid');
  expect(paymentLabel(paid)).toBe('80 PLN · Paid');
});
