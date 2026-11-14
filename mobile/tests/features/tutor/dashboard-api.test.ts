import { describe, expect, it, vi } from 'vitest';
import { loadToday } from '../../../src/features/tutor/today/api/load-today';
import { unpaidLabel } from '../../../src/features/tutor/workspace/models/workspace-format';
import type { ApiClient } from '../../../src/shared/api/api-client';

describe('tutor dashboard API', () => {
  it('loads upcoming tutor lessons, lessons awaiting an outcome, and unpaid amounts by currency', async () => {
    const now = new Date(2026, 10, 13, 12);
    const later = new Date(2026, 10, 13, 14).toISOString();
    const end = new Date(2026, 10, 13, 15).toISOString();
    const get = vi.fn(async (path: string) => {
      if (path.includes('awaitingOutcome=true')) return [{ lessonId: 'older' }];
      if (path.includes('/overdue')) return [
        { amountsByCurrency: [{ currency: 'PLN', amount: 80 }] },
        { amountsByCurrency: [{ currency: 'PLN', amount: 40 }, { currency: 'EUR', amount: 30 }] },
      ];
      return [{ lessonId: '1', status: 'Scheduled', startsAtUtc: later, endsAtUtc: end }, { lessonId: '2', status: 'Cancelled', startsAtUtc: later, endsAtUtc: end }];
    });
    const signal = new AbortController().signal;
    const result = await loadToday({ get } as unknown as ApiClient, signal, now);
    expect(result.toClose.map(lesson => lesson.lessonId)).toEqual(['older']);
    expect(result.upcoming.map(lesson => lesson.lessonId)).toEqual(['1']);
    expect(unpaidLabel(result.overdue)).toBe('30 EUR + 120 PLN');
    const calendar = get.mock.calls.find(([path]) => path.startsWith('/api/calendar?'))?.[0];
    expect(calendar).toContain(encodeURIComponent(new Date(2026, 10, 13).toISOString()));
    expect(calendar).toContain(encodeURIComponent(new Date(2026, 10, 14).toISOString()));
    expect(calendar).toContain('tutorOnly=true');
  });

  it('propagates an API failure so the screen can offer a retry', async () => {
    const get = vi.fn().mockRejectedValue(new Error('offline'));
    await expect(loadToday({ get } as unknown as ApiClient, new AbortController().signal)).rejects.toThrow('offline');
  });
});
