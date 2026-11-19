import { afterEach, expect, it, vi } from 'vitest';
import type { ApiClient } from '@/shared/api/api-client';
import { getLessonPage } from '@/shared/lessons/api/get-lesson-page';

afterEach(() => vi.unstubAllEnvs());

it('uses the configured lesson page size for both roles', async () => {
  vi.stubEnv('EXPO_PUBLIC_LESSONS_PAGE_SIZE', '7');
  const get = vi.fn();
  const api = { get } as unknown as ApiClient;
  const signal = new AbortController().signal;
  for (const tutorOnly of [false, true]) {
    await getLessonPage(api, { tutorOnly, page: 2, view: 'all' }, signal);
    const path = get.mock.lastCall![0] as string;
    const params = new URL(path, 'https://example.test').searchParams;
    expect(params.get('pageSize')).toBe('7');
    expect(params.get('page')).toBe('2');
    expect(params.get('tutorOnly')).toBe(String(tutorOnly));
  }
});

it.each([undefined, '', '0', '51', '1.5', 'abc'])('rejects invalid lesson page size %s before sending a request', async value => {
  vi.stubEnv('EXPO_PUBLIC_LESSONS_PAGE_SIZE', value);
  const get = vi.fn();
  await expect(getLessonPage({ get } as unknown as ApiClient,
    { tutorOnly: false, page: 1, view: 'all' }, new AbortController().signal))
    .rejects.toThrow('EXPO_PUBLIC_LESSONS_PAGE_SIZE');
  expect(get).not.toHaveBeenCalled();
});
