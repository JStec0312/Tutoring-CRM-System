import { expect, it, vi } from "vitest";

import { completeGoogleAccountLink } from "@/features/auth/google-link/api/complete-google-account-link";
import { getGoogleLinkError } from "@/features/auth/google-link/models/google-link-error";
import { createApiClient } from "@/shared/api/api-client";
import { ApiError } from "@/shared/api/api-error";

const continuation = () => ({
  continuationToken: "a".repeat(64),
  continuationExpiresAtUtc: new Date(Date.now() + 300_000).toISOString(),
});

it("sends the continuation with the current CRM bearer token and no account ID", async () => {
  const fetcher = vi.fn(async (_url: RequestInfo | URL, _init?: RequestInit) => new Response(null, { status: 204 }));
  const api = createApiClient({
    baseUrl: "https://example.com",
    fetcher,
    session: {
      getGeneration: () => 1,
      assertCurrent: () => {},
      getValidAccessToken: async () => "crm-access-token",
      refreshAfterUnauthorized: async () => null,
    },
  });
  const current = continuation();
  await expect(completeGoogleAccountLink(api, current)).resolves.toBeUndefined();
  const [url, options] = fetcher.mock.calls[0];
  expect(url).toBe("https://example.com/api/auth/google/link");
  expect(options?.method).toBe("POST");
  expect(JSON.parse(String(options?.body))).toEqual({ continuationToken: current.continuationToken });
  expect(new Headers(options?.headers).get("Authorization")).toBe("Bearer crm-access-token");
});

it("does not send a linking request without an authenticated CRM session", async () => {
  const fetcher = vi.fn();
  const api = createApiClient({ baseUrl: "https://example.com", fetcher });
  await expect(completeGoogleAccountLink(api, continuation())).rejects.toMatchObject({ status: 401 });
  expect(fetcher).not.toHaveBeenCalled();
});

it("rejects an expired continuation before sending a request", () => {
  const fetcher = vi.fn();
  const api = createApiClient({ baseUrl: "https://example.com", fetcher });
  expect(() => completeGoogleAccountLink(api, {
    ...continuation(), continuationExpiresAtUtc: "2020-01-01T00:00:00Z",
  })).toThrow("Google.LinkExpired");
  expect(fetcher).not.toHaveBeenCalled();
});

it.each([
  [401, "Auth.InvalidGoogleLinkContinuation", "same email address"],
  [409, "Auth.GoogleAccountLinkConflict", "already connected"],
  [403, "Auth.InactiveAccount.", "inactive"],
  [0, "Network.Unavailable", "connect to the server"],
])("explains HTTP %i / %s", (status, code, message) => {
  expect(getGoogleLinkError(new ApiError(Number(status), String(code)))).toContain(message);
});
