import { expect, it, vi } from "vitest";

import { createGoogleSignInChallenge } from "@/features/auth/sign-in/api/create-google-sign-in-challenge";
import { createApiClient } from "@/shared/api/api-client";

it("requests a Google challenge without a CRM session", async () => {
  const challenge = {
    nonce: "a".repeat(64),
    expiresAtUtc: "2026-11-18T12:05:00Z",
  };
  const fetcher = vi.fn(async (_url: RequestInfo | URL, _options?: RequestInit) =>
    new Response(JSON.stringify(challenge)),
  );
  const api = createApiClient({ baseUrl: "https://example.com", fetcher });

  await expect(createGoogleSignInChallenge(api)).resolves.toEqual(challenge);

  const [url, options] = fetcher.mock.calls[0];
  expect(url).toBe("https://example.com/api/auth/google/challenge");
  expect(options?.method).toBe("POST");
  expect(JSON.parse(String(options?.body))).toEqual({});
  expect(new Headers(options?.headers).has("Authorization")).toBe(false);
});

it("propagates a failed challenge request", async () => {
  const api = createApiClient({
    baseUrl: "https://example.com",
    fetcher: async () => new Response("", { status: 503 }),
  });

  await expect(createGoogleSignInChallenge(api)).rejects.toMatchObject({ status: 503 });
});
