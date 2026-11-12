import { ApiError } from "../api/api-error";
import type { RefreshTokenStorage } from "./refresh-token-storage.types";
import type {
    IssuedSession,
    LoginCredentials,
    SessionApi,
} from "./session-api";
import { InvalidSessionResponseError } from "./session-api";
import type { AuthStatus } from "./session-types";
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
  now = Date.now,
}: Options) {
  let generation = 0;
  let accessToken: AccessToken | null = null;

  let snapshot: Snapshot = {
    status: "restoring",
    error: null,
  };

  let queue: Promise<void> = Promise.resolve();
  let refreshFlight: Promise<void> | null = null;
  let restoreFlight: Promise<void> | null = null;
  let loginRunning = false;

  const listeners = new Set<() => void>();

  function publish(next: Snapshot) {
    snapshot = next;
    listeners.forEach((listener) => listener());
  }

  function assertCurrent(expected: number) {
    if (expected !== generation) {
      throw new SessionChangedError();
    }
  }

  function clearLocal(error: string | null = null) {
    generation += 1;
    accessToken = null;

    publish({
      status: "anonymous",
      error,
    });
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

  async function commitIssued(issued: IssuedSession, expected: number) {
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
        if (await storage.isLogoutPending()) {
          await revokeStoredToken();
        }

        assertCurrent(expected);

        const issued = await api.login(credentials);
        await commitIssued(issued, expected);
      });
    } finally {
      loginRunning = false;
    }
  }

  function restoreSession(): Promise<void> {
    if (restoreFlight) return restoreFlight;

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

      if (await storage.isLogoutPending()) {
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
        if (generation === expected && invalidCredential(error)) {
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
      if (generation === expected) {
        publish({
          status: "restore-error",
          error: errorCode(error),
        });
      }
    });

    restoreFlight = task.finally(() => {
      restoreFlight = null;
    });

    return restoreFlight;
  }

  function refreshSession(): Promise<void> {
    if (refreshFlight) return refreshFlight;

    if (snapshot.status !== "authenticated") {
      return Promise.reject(new Error("Session.Required"));
    }

    const expected = generation;

    const task = enqueue(async () => {
      assertCurrent(expected);

      if (await storage.isLogoutPending()) {
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
        if (generation === expected && invalidCredential(error)) {
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
    });

    refreshFlight = task.finally(() => {
      refreshFlight = null;
    });

    return refreshFlight;
  }

  async function getValidAccessToken(): Promise<string | null> {
    if (snapshot.status === "restoring") {
      await restoreSession();
    }

    if (snapshot.status === "restore-error") {
      throw new Error("Session.RestoreFailed");
    }

    if (snapshot.status !== "authenticated") {
      return null;
    }

    const expected = generation;

    if (await storage.isLogoutPending()) {
      if (generation === expected) {
        clearLocal("Session.LogoutPending");
      }

      return null;
    }

    assertCurrent(expected);

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
    if (snapshot.status !== "authenticated") {
      return null;
    }

    const expected = generation;
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

    return enqueue(async () => {
      // Keep the marker if server revocation fails.
      await storage.setLogoutPending(true);
      await revokeStoredToken();
    });
  }

  return {
    getSnapshot: () => snapshot,

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
