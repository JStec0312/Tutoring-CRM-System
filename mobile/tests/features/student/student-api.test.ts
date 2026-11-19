import { describe, expect, it, vi } from 'vitest';
import type { ApiClient } from '@/shared/api/api-client';
import { loadStudentOverview } from '@/features/student/overview/api/load-overview';
import { getStudentCalendar } from '@/features/student/calendar/api/get-student-calendar';
import { outstandingByCurrency, type StudentCharge } from '@/features/student/payments/api/charges';
import { validateMaterial } from '@/features/student/materials/models/validate-material';
import { getStudentNotifications } from '@/features/student/notifications/api/get-notifications';

describe('student workspace data', () => {
  it('loads agreements without lessons and keeps charges in their agreement', async () => {
    const get = vi.fn(async (path: string) => {
      if (path === '/api/students/me/agreements') return [{ agreementId: 'math' }, { agreementId: 'physics' }];
      if (path.startsWith('/api/calendar?')) return [
        { lessonId: 'cancelled', tutoringAgreementId: 'math', status: 'Cancelled' },
        { lessonId: 'next', tutoringAgreementId: 'math', status: 'Scheduled' },
      ];
      if (path.includes('/math/charges')) return [{ currency: 'PLN', amount: 80, paidAtUtc: null }];
      if (path.includes('/physics/charges')) return [];
      if (path.includes('tutoringAgreementId=math')) return [{ materialId: 'math-file' }];
      if (path.includes('tutoringAgreementId=physics')) return [];
      throw new Error(path);
    });
    const result = await loadStudentOverview({ get } as unknown as ApiClient, new AbortController().signal);
    expect(result.next?.lessonId).toBe('next');
    expect(result.summaries).toHaveLength(2);
    expect(result.summaries[1].next).toBeUndefined();
    expect(result.summaries[0].charges[0].amount).toBe(80);
    expect(get.mock.calls.some(([path]) => path.startsWith('/api/learning-materials'))).toBe(false);
    expect(get.mock.calls.every(([path]) => !path.startsWith('/api/tutors/'))).toBe(true);
  });

  it('uses UTC range boundaries without tutor-only permissions', async () => {
    const get = vi.fn().mockResolvedValue([]);
    const signal = new AbortController().signal;
    const from = new Date('2026-11-01T00:00:00+01:00');
    const to = new Date('2026-11-08T00:00:00+01:00');
    await getStudentCalendar({ get } as unknown as ApiClient, from, to, signal);
    const params = new URL(get.mock.calls[0][0], 'https://example.com').searchParams;
    expect(params.get('from')).toBe('2026-10-31T23:00:00.000Z');
    expect(params.get('to')).toBe('2026-11-07T23:00:00.000Z');
    expect(params.has('tutorOnly')).toBe(false);
    expect(get.mock.calls[0][1]).toEqual({ signal });
  });

  it('keeps currencies separate and excludes paid charges', () => {
    const charges = [
      { currency: 'PLN', amount: 80, paidAtUtc: null },
      { currency: 'PLN', amount: 40, paidAtUtc: null },
      { currency: 'EUR', amount: 20, paidAtUtc: null },
      { currency: 'EUR', amount: 300, paidAtUtc: '2026-11-01T12:00:00Z' },
    ] as StudentCharge[];
    expect(outstandingByCurrency(charges)).toEqual([['EUR', 20], ['PLN', 120]]);
  });

  it('reports only upcoming scheduled and cancelled lessons in notifications', async () => {
    const get = vi.fn().mockResolvedValue([
      { lessonId: 'past', status: 'Scheduled', startsAtUtc: '2026-11-18T10:00:00Z' },
      { lessonId: 'upcoming', status: 'Scheduled', startsAtUtc: '2026-11-19T10:00:00Z' },
      { lessonId: 'done', status: 'Completed', startsAtUtc: '2026-11-19T10:00:00Z' },
      { lessonId: 'cancelled', status: 'Cancelled', startsAtUtc: '2026-11-17T10:00:00Z' },
    ]);
    const result = await getStudentNotifications({ get } as unknown as ApiClient, new AbortController().signal, new Date('2026-11-18T12:00:00Z'));
    expect(result.map(lesson => lesson.lessonId)).toEqual(['upcoming', 'cancelled']);
  });

  it('propagates a failed overview request for the retry state', async () => {
    const api = { get: vi.fn().mockRejectedValue(new Error('offline')) } as unknown as ApiClient;
    await expect(loadStudentOverview(api, new AbortController().signal)).rejects.toThrow('offline');
  });
});

describe('material selection', () => {
  it.each([
    ['notes.pdf', 20_000_000, true], ['solution.PNG', 15, true], ['photo.jpg', undefined, true],
    ['empty.pdf', 0, false], ['large.pdf', 20_000_001, false], ['program.exe', 100, false],
  ])('validates %s with size %s', (name, size, valid) => {
    expect(validateMaterial({ name, size }) === '').toBe(valid);
  });
});
