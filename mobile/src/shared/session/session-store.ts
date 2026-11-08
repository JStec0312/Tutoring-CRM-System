import { SessionChangedError, SessionSnapshot, SessionStatus, AccessToken, User } from './session-types';

export function createSessionStore() {
  let generation = 0;
  let token: AccessToken | null = null;
  let snapshot: SessionSnapshot = { status: SessionStatus.Restoring, user: null, error: null };
  const initialSnapshot = snapshot;
  const listeners = new Set<() => void>();
  const cleanup = new Set<() => void>();

  function publish(next: SessionSnapshot) {
    snapshot = next;
    listeners.forEach(listener => listener());
  }

  function assertCurrent(expected: number) {
    if (generation !== expected) throw new SessionChangedError();
  }

  return {
    getSnapshot: () => snapshot,
    getServerSnapshot: () => initialSnapshot,
    getGeneration: () => generation,
    getToken: () => token,
    subscribe(listener: () => void) {
      listeners.add(listener);
      return () => { listeners.delete(listener); };
    },
    onClear(listener: () => void) {
      cleanup.add(listener);
      return () => { cleanup.delete(listener); };
    },
    assertCurrent,
    commit(nextToken: AccessToken, user: User, expected: number) {
      assertCurrent(expected);
      token = nextToken;
      publish({ status: SessionStatus.Authenticated, user, error: null });
    },
    clear() {
      generation++;
      token = null;
      cleanup.forEach(listener => listener());
      publish({ status: SessionStatus.Anonymous, user: null, error: null });
    },
    restoring() {
      publish({ status: SessionStatus.Restoring, user: null, error: null });
    },
    restoreFailed(message: string) {
      publish({ status: SessionStatus.RestoreError, user: null, error: message });
    },
    reportError(message: string) {
      publish({ ...snapshot, error: message });
    },
  };
}

export type SessionStore = ReturnType<typeof createSessionStore>;
