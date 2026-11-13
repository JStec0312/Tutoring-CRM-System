import { describe, expect, it, vi } from "vitest";

import { createApiClient } from "@/shared/api/api-client";
import { createSessionManager, SessionChangedError } from "@/shared/session/session-manager";
import type { SessionChange, SessionSync } from "@/shared/session/session-types";

const credentials = { email: "student@example.com", password: "password" };
type Fetcher = (url: string | URL | RequestInfo, init?: RequestInit) => Promise<Response>;

function issued(account: string) {
  return {
    accessToken: account,
    expiresAt: "2030-01-01T00:00:00Z",
    refreshToken: `refresh-${account}`,
  };
}

function fixture(sync?: SessionSync, exclusive?: <T>(operation: () => Promise<T>) => Promise<T>) {
  let savedToken: string | null = null;
  const api = {
    login: vi.fn(async () => issued("account-a")),
    refresh: vi.fn(async () => issued("rotated")),
    signOut: vi.fn(async () => {}),
  };
  const manager = createSessionManager({
    api,
    sync,
    exclusive,
    now: () => Date.parse("2026-11-05T12:00:00Z"),
    storage: {
      native: !sync,
      readRefreshToken: async () => savedToken,
      writeRefreshToken: async (token) => { savedToken = token; },
      deleteRefreshToken: async () => { savedToken = null; },
      isLogoutPending: async () => false,
      setLogoutPending: async () => {},
    },
  });
  const client = (fetcher: typeof fetch) =>
    createApiClient({ baseUrl: "https://example.test", session: manager, fetcher });

  return { api, manager, client };
}

function browserTabs() {
  let current: SessionChange | null = null;
  let nextId = 0;
  let queue = Promise.resolve();
  const listeners = new Set<() => void>();
  const publish = vi.fn<SessionSync["publish"]>((type) => {
    current = { id: String(++nextId), type };
    return current;
  });
  const sync: SessionSync = {
    read: () => current,
    publish,
    subscribe(listener) {
      listeners.add(listener);
      return () => { listeners.delete(listener); };
    },
  };
  function exclusive<T>(operation: () => Promise<T>) {
    const task = queue.then(operation);
    queue = task.then(() => undefined, () => undefined);
    return task;
  }
  const deliverEvents = () => listeners.forEach((listener) => listener());
  return {
    a: fixture(sync, exclusive),
    b: fixture(sync, exclusive),
    publish,
    deliverEvents,
  };
}

describe("protected HTTP requests", () => {
  it("shares refresh across parallel 401s and reuses the new token for a late 401", async () => {
    const { api, manager, client } = fixture();
    await manager.restoreSession();
    await manager.signIn(credentials);
    const generation = manager.getGeneration();
    const refresh = Promise.withResolvers<ReturnType<typeof issued>>();
    api.refresh.mockReturnValueOnce(refresh.promise);
    const late = Promise.withResolvers<Response>();
    const signal = new AbortController().signal;
    const fetcher = vi.fn<Fetcher>()
      .mockResolvedValueOnce(new Response(null, { status: 401 }))
      .mockResolvedValueOnce(new Response(null, { status: 401 }))
      .mockReturnValueOnce(late.promise)
      .mockImplementation(async () => Response.json({ ok: true }));
    const http = client(fetcher);
    const requests = [
      http.patch("/api/test", { value: 1 }, { signal }),
      http.get("/api/test"),
      http.get("/api/test"),
    ];

    await vi.waitFor(() => expect(api.refresh).toHaveBeenCalledTimes(1));
    refresh.resolve(issued("rotated"));
    await Promise.all(requests.slice(0, 2));
    late.resolve(new Response(null, { status: 401 }));
    await expect(Promise.all(requests)).resolves.toEqual(
      Array(3).fill({ ok: true }),
    );
    expect(api.refresh).toHaveBeenCalledTimes(1);
    expect(manager.getGeneration()).toBe(generation);
    const attempts = fetcher.mock.calls.map(([, init]) => init!);
    const authorization = (init: RequestInit) => new Headers(init.headers).get("Authorization");
    expect(attempts.slice(0, 3).map(authorization)).toEqual(Array(3).fill("Bearer account-a"));
    expect(attempts.slice(3).map(authorization)).toEqual(Array(3).fill("Bearer rotated"));
    const patchAttempts = attempts.filter((attempt) => attempt.method === "PATCH");
    expect(patchAttempts).toHaveLength(2);
    for (const attempt of patchAttempts) {
      expect(attempt.body).toBe('{"value":1}');
      expect(attempt.signal).toBe(signal);
    }
  });

  it.each([200, 401])("discards an old account's HTTP %i while reading the body", async (status) => {
    const { api, manager, client } = fixture();
    await manager.restoreSession();
    await manager.signIn(credentials);
    const body = Promise.withResolvers<string>();
    const response = new Response(null, { status });
    const read = vi.spyOn(response, "text").mockReturnValue(body.promise);
    const fetcher = vi.fn<Fetcher>().mockResolvedValue(response);
    const request = client(fetcher).get("/api/private");
    const rejected = expect(request).rejects.toBeInstanceOf(SessionChangedError);
    await vi.waitFor(() => expect(read).toHaveBeenCalledOnce());
    await manager.signOut();
    api.login.mockResolvedValueOnce(issued("account-b"));
    await manager.signIn(credentials);
    body.resolve('{"private":true}');
    await rejected;
    expect(api.refresh).not.toHaveBeenCalled();
    expect(fetcher).toHaveBeenCalledOnce();
  });

  it.each([
    { status: 401, auth: true, attempts: 2 },
    { status: 401, auth: false, attempts: 1 },
    { status: 403, auth: true, attempts: 1 },
    { status: 0, auth: true, attempts: 1 },
  ])("returns status $status with auth=$auth after $attempts attempt(s)", async ({ status, auth, attempts }) => {
    const { api, manager, client } = fixture();
    await manager.restoreSession();
    await manager.signIn(credentials);
    const fetcher = vi.fn<Fetcher>(async () => {
      if (!status) throw new Error("Offline");
      return Response.json({ code: "Test.Failure" }, { status });
    });
    await expect(client(fetcher).post("/api/test", {}, { auth })).rejects.toMatchObject({
      status,
      code: status ? "Test.Failure" : "Network.Unavailable",
    });
    expect(fetcher).toHaveBeenCalledTimes(attempts);
    expect(api.refresh).toHaveBeenCalledTimes(attempts - 1);
  });

  it.each(["response", "refresh"])("does not replay when aborted during %s", async (phase) => {
    const { api, manager, client } = fixture();
    await manager.restoreSession();
    await manager.signIn(credentials);
    const controller = new AbortController();
    const response = Promise.withResolvers<Response>();
    const refresh = Promise.withResolvers<ReturnType<typeof issued>>();
    api.refresh.mockReturnValueOnce(refresh.promise);
    const fetcher = vi.fn<Fetcher>().mockReturnValue(response.promise);
    const result = client(fetcher).get("/api/test", { signal: controller.signal });
    const rejected = expect(result).rejects.toMatchObject({ name: "AbortError" });
    await vi.waitFor(() => expect(fetcher).toHaveBeenCalledOnce());
    if (phase === "refresh") {
      response.resolve(new Response(null, { status: 401 }));
      await vi.waitFor(() => expect(api.refresh).toHaveBeenCalledOnce());
    }
    controller.abort();
    response.resolve(new Response(null, { status: 401 }));
    refresh.resolve(issued("rotated"));
    await rejected;
    expect(api.refresh).toHaveBeenCalledTimes(phase === "refresh" ? 1 : 0);
    expect(fetcher).toHaveBeenCalledOnce();
  });
});

describe("web session synchronization", () => {
  async function setup() {
    const tabs = browserTabs();
    tabs.a.api.refresh.mockRejectedValueOnce(new Error("Offline"));
    await tabs.a.manager.restoreSession();
    await tabs.a.manager.signOut();
    const unsubscribe = tabs.b.manager.subscribeToSessionChanges();
    return { ...tabs, unsubscribe };
  }

  it("restores on sign-in and clears on sign-out without publishing or signing out again", async () => {
    const { a, b, deliverEvents, publish, unsubscribe } = await setup();
    await a.manager.signIn(credentials);
    deliverEvents();
    await b.manager.restoreSession();
    expect(await b.manager.getValidAccessToken()).toBe("rotated");
    expect(b.api.refresh).toHaveBeenCalledOnce();
    const logout = Promise.withResolvers<void>();
    a.api.signOut.mockReturnValueOnce(logout.promise);
    const signingOut = a.manager.signOut();
    deliverEvents();
    expect(b.manager.getSnapshot().status).toBe("anonymous");
    logout.resolve();
    await signingOut;
    expect(b.api.signOut).not.toHaveBeenCalled();
    expect(publish.mock.calls).toEqual([["signed-out"], ["signed-in"], ["signed-out"]]);
    unsubscribe();
  });

  it("invalidates a pending cookie restore when a later sign-out arrives", async () => {
    const { a, b, deliverEvents, publish } = await setup();
    const refresh = Promise.withResolvers<ReturnType<typeof issued>>();
    b.api.refresh.mockReturnValueOnce(refresh.promise);
    await a.manager.signIn(credentials);
    deliverEvents();
    const restoring = b.manager.restoreSession();
    await vi.waitFor(() => expect(b.api.refresh).toHaveBeenCalledOnce());
    const signingOut = a.manager.signOut();
    deliverEvents();
    expect(b.manager.getSnapshot().status).toBe("anonymous");
    refresh.resolve(issued("obsolete"));
    await Promise.all([restoring, signingOut]);
    expect(b.manager.getSnapshot().status).toBe("anonymous");
    expect(b.api.signOut).not.toHaveBeenCalled();
    expect(publish).toHaveBeenCalledTimes(3);
  });

  it("does not refresh a different account's cookie when the browser event is delayed", async () => {
    const { a, b, deliverEvents } = await setup();
    await a.manager.signIn(credentials);
    deliverEvents();
    await b.manager.restoreSession();
    const response = Promise.withResolvers<Response>();
    const fetcher = vi.fn<Fetcher>().mockReturnValue(response.promise);
    const request = b.client(fetcher).get("/api/private");
    const rejected = expect(request).rejects.toBeInstanceOf(SessionChangedError);
    await vi.waitFor(() => expect(fetcher).toHaveBeenCalledOnce());
    await a.manager.signOut();
    a.api.login.mockResolvedValueOnce(issued("account-b"));
    await a.manager.signIn(credentials);
    response.resolve(new Response(null, { status: 401 }));
    await rejected;
    expect(fetcher).toHaveBeenCalledOnce();
    await b.manager.restoreSession();
    expect(b.api.refresh).toHaveBeenCalledTimes(2);
  });
});
