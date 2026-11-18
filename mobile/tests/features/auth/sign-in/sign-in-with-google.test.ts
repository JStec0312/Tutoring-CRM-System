import { expect, it, vi } from "vitest";

import { signInWithGoogle } from "@/features/auth/sign-in/api/sign-in-with-google";
import { getGoogleSignInError } from "@/features/auth/sign-in/models/google-sign-in-error";
import { ApiError } from "@/shared/api/api-error";
import { createApiClient } from "@/shared/api/api-client";
import { InvalidSessionResponseError } from "@/shared/session/session-api";

const futureDate = () => new Date(Date.now() + 3_600_000).toISOString();

function fixture(body: unknown) {
  const fetcher = vi.fn(async (_url: RequestInfo | URL, _init?: RequestInit) => Response.json(body));
  const api = createApiClient({ baseUrl: "https://example.com", credentials: "include", fetcher });
  return { api, fetcher };
}

it.each([false, true])("exchanges the ID token with useNativeSession=%s", async (native) => {
  const expiresAt = futureDate();
  const { api, fetcher } = fixture({
    status: "signedIn", accessToken: "crm-access", expiresAt,
    refreshToken: "crm-refresh", refreshTokenExpiresAt: futureDate(),
  });

  const response = await signInWithGoogle(api, "google-id-token", native);

  expect(response).toEqual({
    status: "signedIn",
    session: { accessToken: "crm-access", expiresAt, refreshToken: native ? "crm-refresh" : null },
  });
  const [url, options] = fetcher.mock.calls[0];
  expect(url).toBe("https://example.com/api/auth/google/sign-in");
  expect(options?.method).toBe("POST");
  expect(options?.credentials).toBe("include");
  expect(JSON.parse(String(options?.body))).toEqual({ idToken: "google-id-token", useNativeSession: native });
  expect(new Headers(options?.headers).has("Authorization")).toBe(false);
});

it("accepts a browser session without a JSON refresh token and normalizes backend UTC dates", async () => {
  const expiresAt = futureDate();
  const { api } = fixture({ status: "signedIn", accessToken: "crm-access", expiresAt: expiresAt.replace("Z", "") });
  await expect(signInWithGoogle(api, "google-token", false)).resolves.toEqual({
    status: "signedIn", session: { accessToken: "crm-access", expiresAt, refreshToken: null },
  });
});

it.each(["linkRequired", "registrationRequired"])("returns %s without accepting it as a session", async (status) => {
  const body: Record<string, unknown> = { status, continuationToken: "a".repeat(64), continuationExpiresAtUtc: futureDate() };
  if (status === "linkRequired") body.email = "matching@example.com";
  await expect(signInWithGoogle(fixture(body).api, "google-token", false)).resolves.toEqual(body);
});

it.each([
  null,
  { status: "unknown" },
  { status: "linkRequired", continuationToken: "a".repeat(64), continuationExpiresAtUtc: futureDate() },
  { status: "signedIn", accessToken: "crm", expiresAt: "2020-01-01T00:00:00Z" },
  { status: "signedIn", accessToken: "crm", expiresAt: "invalid" },
  { status: "linkRequired", continuationToken: "short", continuationExpiresAtUtc: futureDate() },
  { status: "registrationRequired", continuationToken: "a".repeat(64), continuationExpiresAtUtc: "2020-01-01T00:00:00Z" },
])("rejects malformed or expired responses: %j", async (body) => {
  await expect(signInWithGoogle(fixture(body).api, "google-token", false)).rejects.toBeInstanceOf(InvalidSessionResponseError);
});

it("rejects a native session without durable refresh credentials", async () => {
  const { api } = fixture({ status: "signedIn", accessToken: "crm", expiresAt: futureDate() });
  await expect(signInWithGoogle(api, "google-token", true)).rejects.toBeInstanceOf(InvalidSessionResponseError);
});

it.each([
  [401, "Auth.InvalidGoogleSignIn", "invalid or expired"],
  [401, "Auth.GoogleVerifiedEmailRequired", "verified email"],
  [403, "Auth.InactiveAccount.", "inactive"],
  [503, "Auth.GoogleSignInUnavailable", "temporarily unavailable"],
  [429, "Http.429", "Too many"],
])("shows a readable message for HTTP %i / %s", async (status, code, message) => {
  const api = createApiClient({
    baseUrl: "https://example.com",
    fetcher: async () => Response.json({ code }, { status: Number(status) }),
  });
  await expect(signInWithGoogle(api, "google-token", false).catch(getGoogleSignInError)).resolves.toContain(message);
});

it("handles network and session storage failures", () => {
  expect(getGoogleSignInError(new ApiError(0, "Network.Unavailable"))).toContain("connect to the server");
  expect(getGoogleSignInError(new Error("Session.StorageFailed"))).toContain("save your session");
});
