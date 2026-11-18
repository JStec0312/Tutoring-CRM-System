import { expect, it, vi } from "vitest";

import { registerWithGoogle } from "@/features/auth/google-register/api/register-with-google";
import { getGoogleRegistrationError } from "@/features/auth/google-register/models/google-registration-error";
import type { GoogleContinuation } from "@/features/auth/sign-in/models/google-continuation";
import { createApiClient } from "@/shared/api/api-client";
import { ApiError } from "@/shared/api/api-error";
import { InvalidSessionResponseError } from "@/shared/session/session-api";

const futureDate = () => new Date(Date.now() + 300_000).toISOString();
const continuation = (): GoogleContinuation => ({
  status: "registrationRequired", continuationToken: "a".repeat(64), continuationExpiresAtUtc: futureDate(),
});

it.each([
  ["student", "Student", false],
  ["tutor", "Tutor", true],
] as const)("registers %s with useNativeSession=%s", async (role, apiRole, native) => {
  const expiresAt = futureDate();
  const fetcher = vi.fn(async (_url: RequestInfo | URL, _options?: RequestInit) => Response.json({
    status: "signedIn", accessToken: "crm-access", expiresAt,
    refreshToken: "crm-refresh", refreshTokenExpiresAt: futureDate(),
  }, { status: 201 }));
  const api = createApiClient({ baseUrl: "https://example.com", credentials: "include", fetcher });
  const result = await registerWithGoogle(api, continuation(), role, " new-user ", native);
  expect(result.accessToken).toBe("crm-access");
  if (native) {
    expect(result.refreshToken).toBe("crm-refresh");
  } else {
    expect(result.refreshToken).toBeNull();
  }
  const [url, options] = fetcher.mock.calls[0];
  expect(url).toBe("https://example.com/api/auth/google/register");
  expect(options?.method).toBe("POST");
  expect(options?.credentials).toBe("include");
  expect(new Headers(options?.headers).has("Authorization")).toBe(false);
  expect(JSON.parse(String(options?.body))).toEqual({
    continuationToken: "a".repeat(64), role: apiRole, userName: "new-user", useNativeSession: native,
  });
});

it("rejects an expired or wrong-purpose continuation before sending HTTP", async () => {
  const fetcher = vi.fn();
  const api = createApiClient({ baseUrl: "https://example.com", fetcher });
  await expect(registerWithGoogle(api, { ...continuation(), status: "linkRequired" }, "student", "user", false)).rejects.toThrow("Google.RegistrationExpired");
  await expect(registerWithGoogle(api, { ...continuation(), continuationExpiresAtUtc: "2020-01-01T00:00:00Z" }, "student", "user", false)).rejects.toThrow("Google.RegistrationExpired");
  expect(fetcher).not.toHaveBeenCalled();
});

it.each([
  null,
  { status: "registrationRequired" },
  { status: "signedIn", accessToken: "crm", expiresAt: "2020-01-01T00:00:00Z" },
  { status: "signedIn", accessToken: "crm", expiresAt: futureDate() },
])("rejects invalid native session responses: %j", async (body) => {
  const api = createApiClient({ baseUrl: "https://example.com", fetcher: async () => Response.json(body, { status: 201 }) });
  await expect(registerWithGoogle(api, continuation(), "tutor", "user", true)).rejects.toBeInstanceOf(InvalidSessionResponseError);
});

it.each([
  [401, "Auth.InvalidGoogleRegistrationContinuation", "no longer available"],
  [409, "Auth.GoogleRegistrationConflict", "already exists"],
  [409, "Username.AlreadyTaken", "already taken"],
  [0, "Network.Unavailable", "connect to the server"],
  [429, "Http.429", "Too many"],
])("explains HTTP %i / %s", (status, code, message) => {
  expect(getGoogleRegistrationError(new ApiError(Number(status), String(code)))).toContain(message);
});
