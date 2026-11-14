import { describe, expect, it, vi } from 'vitest';
import { getTutorDashboard } from '../../../src/features/tutor/components/dashboard-api';
import type { ApiClient } from '../../../src/shared/api/api-client';

describe('tutor dashboard API', () => {
  it('loads tutor metrics and upcoming scheduled lessons for the local day', async () => {
    const now = new Date(2026, 10, 13, 12);
    const later = new Date(2026, 10, 13, 14).toISOString();
    const end = new Date(2026, 10, 13, 15).toISOString();
    const get = vi.fn(async (path: string) => {
      if (path.includes('completed-count')) return { completedLessonCount: 12 };
      if (path.includes('total-teaching-time')) return { totalTeachingSeconds: 7200 };
      if (path.includes('/overdue')) return [{ unpaidLessonCount: 2 }, { unpaidLessonCount: 1 }];
      return [{ lessonId: '1', status: 'Scheduled', startsAtUtc: later, endsAtUtc: end }, { lessonId: '2', status: 'Cancelled', startsAtUtc: later, endsAtUtc: end }];
    });
    const result = await getTutorDashboard({ get } as unknown as ApiClient, now);
    expect(result).toMatchObject({ completed: 12, teachingSeconds: 7200, unpaidLessons: 3, overdueAgreements: 2 });
    expect(result.lessons.map(lesson => lesson.lessonId)).toEqual(['1']);
    const calendar = get.mock.calls.find(([path]) => path.startsWith('/api/calendar?'))?.[0];
    expect(calendar).toContain(encodeURIComponent(new Date(2026, 10, 13).toISOString()));
    expect(calendar).toContain(encodeURIComponent(new Date(2026, 10, 14).toISOString()));
  });

  it('propagates an API failure so the screen can offer a retry', async () => {
    const get = vi.fn().mockRejectedValue(new Error('offline'));
    await expect(getTutorDashboard({ get } as unknown as ApiClient, new Date())).rejects.toThrow('offline');
  });
});
