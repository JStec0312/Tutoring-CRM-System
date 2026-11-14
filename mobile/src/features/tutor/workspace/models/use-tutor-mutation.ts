import { useRef, useState } from 'react';
import { errorMessage } from './use-tutor-query';

export function useTutorMutation() {
  const inFlight = useRef(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  async function run(action: () => Promise<unknown>) {
    if (inFlight.current) return;
    inFlight.current = true;
    setBusy(true);
    setError('');
    try {
      await action();
    } catch (error) {
      setError(errorMessage(error));
    } finally {
      inFlight.current = false;
      setBusy(false);
    }
  }

  return { run, busy, error, setError };
}
