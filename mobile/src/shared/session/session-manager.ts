import { ApiError } from "../api/api-error";
import type { RefreshTokenStorage } from "./refresh-token-storage.types";
import type {
    IssuedSession,
    LoginCredentials,
    SessionApi,
} from "./session-api";
import { InvalidSessionResponseError } from "./session-api";
import type { AuthStatus, SessionChangeType, SessionSync } from "./session-types";
// the session maanger is an object that manages authentication state and refresh token lifecycle.

type Snapshot = Readonly<{
  status: AuthStatus;
  error: string | null;
}>;

type AccessToken = {
  value: string;
  expiresAt: number;
};

type Exclusive = <T>(operation: () => Promise<T>) => Promise<T>;

type Options = {
  api: SessionApi;
  storage: RefreshTokenStorage;
  exclusive?: Exclusive;
  sync?: SessionSync;
  now?: () => number;
};

const REFRESH_MARGIN_MS = 30_000;

export class SessionChangedError extends Error {
  constructor() {
    super("Session.Changed");
    this.name = "SessionChangedError";
  }
}

function invalidCredential(error: unknown): boolean {
  return (
    (error instanceof ApiError && error.status === 401) ||
    error instanceof InvalidSessionResponseError
  );
}

function errorCode(error: unknown): string {
  return error instanceof ApiError ? error.code : "Session.RestoreFailed";
}

export function createSessionManager({
  api,
  storage,
  exclusive = (operation) => operation(),
  sync,
  now = Date.now,
}: Options) {
  let generation = 0;
  let accessToken: AccessToken | null = null;

  let snapshot: Snapshot = {
    status: "restoring",
    error: null,
  };

  let queue: Promise<void> = Promise.resolve();
  let refreshFlight: { generation: number; task: Promise<void> } | null = null;
  let restoreFlight: { generation: number; task: Promise<void> } | null = null;
  let loginRunning = false;
  let lastChangeId: string | undefined;

  const listeners = new Set<() => void>();

  function publish(next: Snapshot) {
    snapshot = next;
    listeners.forEach((listener) => listener());
  }

  function synchronizeSession() {
    // Read under the credential lock too, since storage events arrive asynchronously.
    const change = sync?.read();
    if (change && change.id !== lastChangeId) {
      lastChangeId = change.id;
      if (change.type === "signed-in") {
        resetLocal("restoring");
        void restoreSession();
      } else {
        clearLocal();
      }
    }

    return generation;
  }

  function announce(type: SessionChangeType) {
    const change = sync?.publish(type);
    if (change) {
      lastChangeId = change.id;
    }
    return change;
  }

  function assertCurrent(expected: number) {
    if (expected !== synchronizeSession()) {
      throw new SessionChangedError();
    }
  }

  function resetLocal(status: AuthStatus, error: string | null = null) {
    generation += 1;
    accessToken = null;

    publish({
      status,
      error,
    });
  }

  function clearLocal(error: string | null = null) {
    resetLocal("anonymous", error);
  }

  // Serializes operations that modify refresh credentials.
  function enqueue<T>(operation: () => Promise<T>): Promise<T> {
    const result = queue.then(() => exclusive(operation));

    // A failed operation must not block the queue forever.
    queue = result.then(
      () => undefined,
      () => undefined,
    );

    return result;
  }

  async function revokeStoredToken() {
    const refreshToken = storage.native
      ? await storage.readRefreshToken()
      : null;

    if (!storage.native || refreshToken) {
      await api.signOut(refreshToken);
    }

    await storage.deleteRefreshToken();
    await storage.setLogoutPending(false);
  }

  async function commitIssued(
    issued: IssuedSession,
    expected: number,
    signingIn = false,
  ) {
    if (storage.native) {
      if (!issued.refreshToken) {
        throw new InvalidSessionResponseError();
      }

      try {
        // Save the rotated token before checking generation.
        // A queued logout must revoke the newest token.
        await storage.writeRefreshToken(issued.refreshToken);
      } catch {
        // Never accept a session without durable credentials.
        await api.signOut(issued.refreshToken).catch(() => undefined);

        if (generation === expected) {
          clearLocal("Session.StorageFailed");
        }

        throw new Error("Session.StorageFailed");
      }
    }

    assertCurrent(expected);
    if (signingIn) {
      announce("signed-in");
    }

    accessToken = {
      value: issued.accessToken,
      expiresAt: Date.parse(issued.expiresAt),
    };

    publish({
      status: "authenticated",
      error: null,
    });
  }

  async function signIn(credentials: LoginCredentials): Promise<void> {
    synchronizeSession();
    if (loginRunning || snapshot.status !== "anonymous") {
      throw new Error("Session.SignInNotAllowed");
    }

    loginRunning = true;
    clearLocal();

    const expected = generation;

    try {
      await enqueue(async () => {
        assertCurrent(expected);

        // Finish a previously interrupted logout first.
        const logoutPending = await storage.isLogoutPending();
        assertCurrent(expected);
        if (logoutPending) {
          await revokeStoredToken();
        }

        assertCurrent(expected);

        const issued = await api.login(credentials);
        await commitIssued(issued, expected, true);
      });
    } finally {
      loginRunning = false;
    }
  }

  function restoreSession(): Promise<void> {
    if (restoreFlight?.generation === generation) return restoreFlight.task;

    if (
      snapshot.status !== "restoring" &&
      snapshot.status !== "restore-error"
    ) {
      return Promise.resolve();
    }

    const expected = generation;

    publish({
      status: "restoring",
      error: null,
    });

    const task = enqueue(async () => {
      assertCurrent(expected);

      const logoutPending = await storage.isLogoutPending();
      assertCurrent(expected);
      if (logoutPending) {
        await revokeStoredToken();
        assertCurrent(expected);
        clearLocal();
        return;
      }

      const refreshToken = await storage.readRefreshToken();

      assertCurrent(expected);

      if (storage.native && !refreshToken) {
        clearLocal();
        return;
      }

      try {
        const issued = await api.refresh(refreshToken);
        await commitIssued(issued, expected);
      } catch (error) {
        if (synchronizeSession() === expected && invalidCredential(error)) {
          clearLocal();

          try {
            await storage.deleteRefreshToken();
          } catch {
            publish({
              status: "anonymous",
              error: "Session.StorageFailed",
            });
          }

          return;
        }

        throw error;
      }
    }).catch((error) => {
      if (synchronizeSession() === expected) {
        publish({
          status: "restore-error",
          error: errorCode(error),
        });
      }
    }).finally(() => {
      if (restoreFlight?.task === task) restoreFlight = null;
    });

    restoreFlight = { generation: expected, task };
    return task;
  }

  function refreshSession(): Promise<void> {
    const expected = synchronizeSession();
    if (refreshFlight?.generation === expected) return refreshFlight.task;

    if (snapshot.status !== "authenticated") {
      return Promise.reject(new Error("Session.Required"));
    }

    const task = enqueue(async () => {
      assertCurrent(expected);

      const logoutPending = await storage.isLogoutPending();
      assertCurrent(expected);
      if (logoutPending) {
        clearLocal("Session.LogoutPending");
        throw new Error("Session.LogoutPending");
      }

      const refreshToken = await storage.readRefreshToken();

      assertCurrent(expected);

      if (storage.native && !refreshToken) {
        clearLocal();
        throw new Error("Session.Required");
      }

      try {
        const issued = await api.refresh(refreshToken);
        await commitIssued(issued, expected);
      } catch (error) {
        if (synchronizeSession() === expected && invalidCredential(error)) {
          clearLocal();

          try {
            await storage.deleteRefreshToken();
          } catch {
            publish({
              status: "anonymous",
              error: "Session.StorageFailed",
            });
          }
        }

        throw error;
      }
    }).finally(() => {
      if (refreshFlight?.task === task) refreshFlight = null;
    });

    refreshFlight = { generation: expected, task };
    return task;
  }

  async function getValidAccessToken(): Promise<string | null> {
    const expected = synchronizeSession();
    if (snapshot.status === "restoring") {
      await restoreSession();
      assertCurrent(expected);
    }

    if (snapshot.status === "restore-error") {
      throw new Error("Session.RestoreFailed");
    }

    if (snapshot.status !== "authenticated") {
      return null;
    }

    const logoutPending = await storage.isLogoutPending();
    assertCurrent(expected);
    if (logoutPending) {
      clearLocal("Session.LogoutPending");
      return null;
    }

    if (!accessToken || accessToken.expiresAt <= now() + REFRESH_MARGIN_MS) {
      await refreshSession();
    }

    assertCurrent(expected);

    return accessToken?.value ?? null;
  }

  // Used when a protected endpoint returns 401.
  async function refreshAfterUnauthorized(
    failedToken: string,
  ): Promise<string | null> {
    const expected = synchronizeSession();
    if (snapshot.status !== "authenticated") {
      return null;
    }

    const current = accessToken?.value;

    // Another request may have refreshed the token already.
    if (current && current !== failedToken) {
      return current;
    }

    await refreshSession();
    assertCurrent(expected);

    return accessToken?.value ?? null;
  }

  function signOut(): Promise<void> {
    // Immediately hide authenticated UI.
    clearLocal();
    const change = announce("signed-out");

    return enqueue(async () => {
      if (change && sync?.read()?.id !== change.id) return;

      // Keep the marker if server revocation fails.
      await storage.setLogoutPending(true);
      await revokeStoredToken();
    });
  }

  return {
    getSnapshot: () => snapshot,
    getGeneration: () => generation,
    assertCurrent,

    subscribeToSessionChanges() {
      const unsubscribe = sync?.subscribe(synchronizeSession);
      synchronizeSession();
      return () => unsubscribe?.();
    },

    subscribe(listener: () => void) {
      listeners.add(listener);

      return () => {
        listeners.delete(listener);
      };
    },

    signIn,
    signOut,
    restoreSession,
    refreshSession,
    getValidAccessToken,
    refreshAfterUnauthorized,
  };
}

export type SessionManager = ReturnType<typeof createSessionManager>;
