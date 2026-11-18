import { useFocusEffect } from 'expo-router';
import { useCallback, useState } from 'react';
import { useApi } from '@/shared/api/api-context';
import type { ApiClient } from '@/shared/api/api-client';
import { ApiError } from '@/shared/api/api-error';

type QueryState<T> = { data?: T; error?: string; loading: boolean };

export function useWorkspaceQuery<T>(load: (api: ApiClient, signal: AbortSignal) => Promise<T>) {
  const api = useApi();
  const [attempt, setAttempt] = useState(0);
  const [state, setState] = useState<QueryState<T>>({ loading: true });
  const reload = useCallback(() => setAttempt(value => value + 1), []);

  useFocusEffect(useCallback(() => {
    const controller = new AbortController();
    setState({ loading: true });

    void load(api, controller.signal).then(data => {
      if (!controller.signal.aborted) setState({ data, loading: false });
    }).catch(error => {
      if (!controller.signal.aborted) setState({ error: errorMessage(error), loading: false });
    });
    return () => controller.abort();
  // eslint-disable-next-line react-hooks/exhaustive-deps -- Changing the attempt restarts the request while focused.
  }, [api, attempt, load]));

  return { ...state, reload };
}

export function errorMessage(error: unknown) {
  if (error instanceof ApiError) {
    const { errors, detail, title } = error.problem ?? {};
    if (errors) return Object.values(errors).flat().join(' ');
    if (error.status >= 400 && error.status < 500) return detail || title || 'Check the details and try again.';
  }
  return 'Something went wrong. Please try again.';
}
