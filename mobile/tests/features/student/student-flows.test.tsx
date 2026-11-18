// @vitest-environment jsdom
import { useEffect } from 'react';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import type { ApiClient } from '@/shared/api/api-client';
import { ApiProvider } from '@/shared/api/api-context';

const { replace, push } = vi.hoisted(() => ({ replace: vi.fn(), push: vi.fn() }));
vi.mock('react-native', () => vi.importActual('react-native-web'));
vi.mock('react-native-safe-area-context', async () => {
  const { View } = await vi.importActual<typeof import('react-native')>('react-native-web');
  return { SafeAreaView: View };
});
vi.mock('expo-router', () => ({
  useRouter: () => ({ replace, push }),
  useLocalSearchParams: () => ({ lessonId: 'lesson-1' }),
  useFocusEffect: (effect: Parameters<typeof useEffect>[0]) => useEffect(effect, [effect]),
}));

import { CancelStudentLessonScreen } from '@/features/student/lesson/components/cancel-lesson-screen';
import { StudentSettlementsScreen } from '@/features/student/payments/components/settlements-screen';
import { ApiError } from '@/shared/api/api-error';

beforeEach(() => vi.clearAllMocks());
afterEach(cleanup);

function renderScreen(element: React.ReactNode, get: ApiClient['get'], post = vi.fn().mockResolvedValue(undefined)) {
  const api = { get, post } as unknown as ApiClient;
  return render(<ApiProvider client={api}>{element}</ApiProvider>);
}

const lesson = { lessonId: 'lesson-1', subject: 'Mathematics', status: 'Scheduled',
  startsAtUtc: '2026-11-19T14:00:00Z', endsAtUtc: '2026-11-19T15:00:00Z' };

it('requires a reason and confirmation before cancelling, then opens refreshed details', async () => {
  const post = vi.fn().mockResolvedValue(undefined);
  renderScreen(<CancelStudentLessonScreen />, vi.fn().mockResolvedValue(lesson), post);
  const review = await screen.findByRole('button', { name: 'Review cancellation' });
  expect(review.getAttribute('aria-disabled')).toBe('true');
  fireEvent.change(screen.getByRole('textbox', { name: 'Cancellation reason' }), { target: { value: '  School trip  ' } });
  fireEvent.click(review);
  expect(post).not.toHaveBeenCalled();
  expect(screen.getByRole('alert').textContent).toContain('School trip');
  fireEvent.click(screen.getByRole('button', { name: 'Confirm cancellation' }));
  await waitFor(() => expect(post).toHaveBeenCalledWith('/api/lessons/lesson-1/cancel', { reason: 'School trip' }));
  await waitFor(() => expect(replace).toHaveBeenCalledWith({ pathname: '/student/lessons/[lessonId]', params: { lessonId: 'lesson-1' } }));
});

it('keeps the cancellation form and reason when the API rejects the request', async () => {
  const post = vi.fn().mockRejectedValue(new ApiError(409, 'Lessons.Changed', { detail: 'The lesson has changed.' }));
  renderScreen(<CancelStudentLessonScreen />, vi.fn().mockResolvedValue(lesson), post);
  fireEvent.change(await screen.findByRole('textbox', { name: 'Cancellation reason' }), { target: { value: 'School trip' } });
  fireEvent.click(screen.getByRole('button', { name: 'Review cancellation' }));
  fireEvent.click(screen.getByRole('button', { name: 'Confirm cancellation' }));
  await screen.findByText('The lesson has changed.');
  expect(replace).not.toHaveBeenCalled();
  expect((screen.getByRole('textbox', { name: 'Cancellation reason' }) as HTMLTextAreaElement).value).toBe('School trip');
});

it('switches charge requests by agreement and keeps currencies separate', async () => {
  const get = vi.fn(async (path: string) => {
    if (path === '/api/students/me/agreements') return [
      { agreementId: 'math', subject: 'Mathematics', tutorFirstName: 'Maria', tutorLastName: null },
      { agreementId: 'physics', subject: 'Physics', tutorFirstName: 'Piotr', tutorLastName: null },
    ];
    if (path.endsWith('/math/charges')) return [
      { chargeId: 'one', lessonId: 'lesson-1', startsAtUtc: lesson.startsAtUtc, amount: 80, currency: 'PLN', paidAtUtc: null },
      { chargeId: 'two', lessonId: 'lesson-1', startsAtUtc: lesson.startsAtUtc, amount: 10, currency: 'EUR', paidAtUtc: null },
    ];
    if (path.endsWith('/physics/charges')) return [];
    throw new Error(path);
  });
  renderScreen(<StudentSettlementsScreen />, get as ApiClient['get']);
  await screen.findByText('Outstanding: 10 EUR + 80 PLN');
  fireEvent.click(screen.getByRole('button', { name: 'Maria / Mathematics' }));
  fireEvent.click(screen.getByRole('button', { name: 'Piotr / Physics' }));
  await screen.findByText('No charges for this agreement.');
  expect(get).toHaveBeenCalledWith('/api/students/me/agreements/physics/charges', expect.objectContaining({ signal: expect.any(AbortSignal) }));
  expect(screen.queryByText('Outstanding: 10 EUR + 80 PLN')).toBeNull();
});

it('retries a failed lesson read before showing the form', async () => {
  const get = vi.fn().mockRejectedValueOnce(new Error('offline')).mockResolvedValue(lesson);
  renderScreen(<CancelStudentLessonScreen />, get);
  fireEvent.click(await screen.findByRole('button', { name: 'Try again' }));
  await screen.findByRole('textbox', { name: 'Cancellation reason' });
  expect(get).toHaveBeenCalledTimes(2);
});
