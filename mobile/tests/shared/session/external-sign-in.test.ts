import { expect, it, vi } from "vitest";

import { signInWithGoogle } from "@/features/auth/sign-in/api/sign-in-with-google";
import { ApiError } from "@/shared/api/api-error";
import { createApiClient } from "@/shared/api/api-client";
import { createSessionManager, SessionChangedError } from "@/shared/session/session-manager";
import { getPortalRoute } from "@/shared/session/portal-route";

const futureDate = () => new Date(Date.now() + 3_600_000).toISOString();
const accessToken = "header." + Buffer.from(JSON.stringify({ role: "Tutor" })).toString("base64url") + ".signature";
const session = () => ({ accessToken, expiresAt: futureDate(), refreshToken: "google-refresh" });

async function fixture(native = true, logoutPending = false, storageFails = false) {
  let savedToken: string | null = null;
  const storage = {
    native,
    readRefreshToken: async () => savedToken,
    writeRefreshToken: vi.fn(async (token: string) => {
      if (storageFails) throw new Error("Storage unavailable");
      savedToken = token;
    }),
    deleteRefreshToken: vi.fn(async () => { savedToken = null; }),
    isLogoutPending: vi.fn(async () => logoutPending),
    setLogoutPending: vi.fn(async (pending: boolean) => { logoutPending = pending; }),
  };
  const api = {
    login: vi.fn(async () => session()),
    refresh: vi.fn(async () => { throw new ApiError(401, "Auth.InvalidRefreshToken"); }),
    signOut: vi.fn(async (_token: string | null) => {}),
  };
  const manager = createSessionManager({ api, storage });
  await manager.restoreSession();
  return { manager, api, storage };
}

it.each([false, true])("accepts a Google session using existing session transport (native=%s)", async (native) => {
  const { manager, storage } = await fixture(native);
  const http = createApiClient({
    baseUrl: "https://example.com",
    fetcher: async () => Response.json({
      status: "signedIn", ...session(), refreshTokenExpiresAt: futureDate(),
    }),
  });

  await manager.signInWith(async () => {
    const response = await signInWithGoogle(http, "google-id-token", native);
    if (response.status === "signedIn") return response.session;
    return null;
  });

  expect(manager.getSnapshot().status).toBe("authenticated");
  expect(manager.getSnapshot().roles).toEqual(["Tutor"]);
  expect(getPortalRoute(manager.getSnapshot().roles)).toBe("/tutor");
  expect(await manager.getValidAccessToken()).toBe(accessToken);
  if (native) {
    expect(storage.writeRefreshToken).toHaveBeenCalledWith("google-refresh");
  } else {
    expect(storage.writeRefreshToken).not.toHaveBeenCalled();
  }
});

it("stays anonymous when another authentication step is required", async () => {
  const { manager, storage } = await fixture();
  await manager.signInWith(async () => null);
  expect(manager.getSnapshot().status).toBe("anonymous");
  expect(storage.writeRefreshToken).not.toHaveBeenCalled();
  await manager.signIn({ email: "user@example.com", password: "password" });
  expect(manager.getSnapshot().status).toBe("authenticated");
});

it("allows a retry after a rejected exchange", async () => {
  const { manager } = await fixture();
  await expect(manager.signInWith(async () => { throw new ApiError(401, "Auth.InvalidGoogleSignIn"); })).rejects.toThrow();
  expect(manager.getSnapshot().status).toBe("anonymous");
  await manager.signInWith(async () => session());
  expect(manager.getSnapshot().status).toBe("authenticated");
});

it("revokes the newest native refresh token when sign-out happens during exchange", async () => {
  const { manager, api } = await fixture();
  const pending = Promise.withResolvers<ReturnType<typeof session>>();
  const exchange = vi.fn(() => pending.promise);
  const signingIn = manager.signInWith(exchange);
  const rejected = expect(signingIn).rejects.toBeInstanceOf(SessionChangedError);
  await vi.waitFor(() => expect(exchange).toHaveBeenCalledOnce());
  const signingOut = manager.signOut();
  pending.resolve(session());
  await rejected;
  await signingOut;
  expect(manager.getSnapshot().status).toBe("anonymous");
  expect(api.signOut).toHaveBeenCalledWith("google-refresh");
});

it("does not accept a session when storing the native refresh token fails", async () => {
  const { manager, api } = await fixture(true, false, true);
  await expect(manager.signInWith(async () => session())).rejects.toThrow("Session.StorageFailed");
  expect(manager.getSnapshot().status).toBe("anonymous");
  expect(api.signOut).toHaveBeenCalledWith("google-refresh");
});

it("completes a pending browser sign-out before issuing another session", async () => {
  const { manager, api, storage } = await fixture(false);
  storage.isLogoutPending.mockResolvedValueOnce(true);
  const exchange = vi.fn(async () => session());
  await manager.signInWith(exchange);
  expect(api.signOut.mock.invocationCallOrder[0]).toBeLessThan(exchange.mock.invocationCallOrder[0]);
});
