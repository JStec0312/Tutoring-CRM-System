import { errorMessage, isUnauthorized } from '../../../shared/api/http';
import { CredentialStorage } from '../../../shared/session/credential-storage.types';
import { createSessionStore } from '../../../shared/session/session-store';
import { SessionEvent, SessionSync } from '../../../shared/session/session-sync-types';
import { Credentials, InvalidSessionResponseError, SessionError, SessionStatus, Session } from '../../../shared/session/session-types';
import { AuthApi } from '../api/auth-api';
import { mapSession } from './session-mapper';

export const TokenRefreshLeewayMs = 30_000;
type Options = {
  api: AuthApi;
  storage: CredentialStorage;
  sync: SessionSync;
  configured?: boolean;
  now?: () => number;
};

export function createAuthController({ api, storage, sync, configured = true, now = Date.now }: Options): Session {
  const store = createSessionStore();
  let queue: Promise<unknown> = Promise.resolve();
  let pendingRefresh: { generation: number; promise: Promise<void> } | undefined;
  let pendingRestore: Promise<void> | undefined;

  function restoreSession(): Promise<void> {
    if (pendingRestore) return pendingRestore;
    const status = store.getSnapshot().status;
    // A settled session needs no startup work. RestoreError remains retryable.
    if (status !== SessionStatus.Restoring && status !== SessionStatus.RestoreError) {
      return Promise.resolve();
    }
    if (!configured) {
      store.clear();
      return Promise.resolve();
    }
    const generation = store.getGeneration();
    store.restoring();
    pendingRestore = (async () => {
      try {
        if (await storage.isLogoutPending() || (storage.native && !await storage.readRefreshToken())) {
          store.assertCurrent(generation);
          store.clear();
        } else {
          await refresh();
        }
      } catch (error) {
        if (generation === store.getGeneration()) store.restoreFailed(errorMessage(error));
      }
    })().finally(() => { pendingRestore = undefined; });
    return pendingRestore;
  }

  async function signIn(credentials: Credentials) {
    store.clear();
    const generation = store.getGeneration();
    await exclusive(async () => {
      store.assertCurrent(generation);
      await saveSession(await api.login(credentials), generation, true);
      sync.publish(SessionEvent.Changed);
    });
  }

  async function signOut() {
    store.clear(); // Synchronous: leave protected routes before waiting for network cleanup.
    sync.publish(SessionEvent.SignedOut);
    try {
      await exclusive(async () => {
        // A marker failure must not prevent native credential deletion.
        const markerError: unknown = await storage.setLogoutPending(true).then(() => null, error => error);
        let refreshToken: string | null = null;
        try { refreshToken = await storage.readRefreshToken(); }
        finally { await storage.deleteRefreshToken(); }
        await api.signOut(refreshToken);
        if (markerError) throw markerError;
        await storage.setLogoutPending(false);
      });
    } catch (error) {
      store.reportError(SessionError.Revocation);
      throw error;
    }
  }

  async function ensureAccessToken() {
    if (store.getSnapshot().status !== SessionStatus.Authenticated) {
      throw new Error(SessionError.Required);
    }
    const generation = store.getGeneration();
    const token = store.getToken();
    if (!token || token.expiresAt <= now() + TokenRefreshLeewayMs) await refresh();
    store.assertCurrent(generation);
    const value = store.getToken()?.value;
    if (!value) throw new Error(SessionError.Required);
    return value;
  }

  function refresh(): Promise<void> {
    if (store.getSnapshot().status === SessionStatus.Anonymous) {
      return Promise.reject(new Error(SessionError.Required));
    }
    const generation = store.getGeneration();
    // Concurrent requests share one rotation; a new account gets its own operation.
    if (pendingRefresh?.generation === generation) return pendingRefresh.promise;
    const promise = exclusive(async () => {
      store.assertCurrent(generation);
      if (await storage.isLogoutPending()) throw new Error(SessionError.LogoutPending);
      const refreshToken = await storage.readRefreshToken();
      if (storage.native && !refreshToken) {
        store.clear();
        throw new Error(SessionError.Required);
      }
      try { await saveSession(await api.refresh(refreshToken), generation); }
      catch (error) {
        if (generation === store.getGeneration() && (isUnauthorized(error) || error instanceof InvalidSessionResponseError)) {
          store.clear();
          await clearCredentials(); // already inside the auth queue/lock
        }
        throw error;
      }
    });
    pendingRefresh = { generation, promise };
    void promise.finally(() => {
      if (pendingRefresh?.promise === promise) pendingRefresh = undefined;
    }).catch(() => undefined);
    return promise;
  }

  async function invalidate() {
    store.clear();
    await exclusive(clearCredentials);
  }

  function subscribeToChanges() {
    return sync.subscribe(event => {
      void externalSessionChanged(event === SessionEvent.SignedOut).catch(() => undefined);
    });
  }

  async function externalSessionChanged(signedOut: boolean) {
    store.clear();
    const generation = store.getGeneration();
    if (!signedOut) {
      await pendingRestore;
      store.assertCurrent(generation);
      store.restoring();
      await restoreSession();
    }
  }

  async function saveSession(response: unknown, generation: number, signingIn = false) {
    const result = mapSession(response, storage.native, now());
    if (result.refreshToken) {
      try { await storage.writeRefreshToken(result.refreshToken); }
      catch {
        // Rotation has happened: fail closed and try to revoke the new credential.
        await api.signOut(result.refreshToken).catch(() => undefined);
        if (generation === store.getGeneration()) store.clear();
        try { await storage.setLogoutPending(true); }
        finally { await storage.deleteRefreshToken(); }
        throw new Error(SessionError.Storage);
      }
    }
    // Persist rotation under the auth lock even if logout has started;
    // queued logout then reads/revokes that latest credential. Never publish it.
    store.assertCurrent(generation);
    if (signingIn) {
      try { await storage.setLogoutPending(false); }
      catch {
        await api.signOut(result.refreshToken).catch(() => undefined);
        store.clear();
        await storage.deleteRefreshToken();
        throw new Error(SessionError.Storage);
      }
      store.assertCurrent(generation);
    }
    store.commit(result.token, result.user, generation);
  }

  async function clearCredentials() {
    try { await storage.setLogoutPending(true); }
    finally { await storage.deleteRefreshToken(); }
  }

  function exclusive<T>(operation: () => Promise<T>): Promise<T> {
    // One auth operation at a time; web storage also locks across browser tabs.
    const result = queue.then(() => storage.exclusive(operation));
    queue = result.catch(() => undefined);
    return result;
  }

  return { store, restoreSession, signIn, signOut, ensureAccessToken, refresh, invalidate, subscribeToChanges };
}

