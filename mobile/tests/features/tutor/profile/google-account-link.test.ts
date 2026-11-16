import { describe, expect, it, vi } from "vitest";

import { createGoogleLinkChallenge, linkGoogleAccount } from "@/features/tutor/profile/api/google-account-link";
import { getGoogleLinkError } from "@/features/tutor/profile/models/google-link-error";
import { createApiClient } from "@/shared/api/api-client";
import { ApiError } from "@/shared/api/api-error";
import { SessionChangedError } from "@/shared/session/session-manager";

const nonce = "AB".repeat(32);
const signal = new AbortController().signal;

describe("Google account linking", () => {
  it("makes both requests using the active CRM session", async () => {
    const send = vi.fn<(url: RequestInfo | URL, init?: RequestInit) => Promise<Response>>()
      .mockResolvedValueOnce(Response.json({ nonce }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }));
    const api = createApiClient({
      baseUrl: "https://crm.test",
      fetcher: send,
      session: {
        getGeneration: () => 1,
        assertCurrent: vi.fn(),
        getValidAccessToken: vi.fn().mockResolvedValue("crm-access-token"),
        refreshAfterUnauthorized: vi.fn(),
      },
    });

    await expect(createGoogleLinkChallenge(api, signal)).resolves.toBe(nonce);
    await linkGoogleAccount(api, "google-id-token", signal);

    const [challenge, link] = send.mock.calls;
    expect(challenge[0]).toBe("https://crm.test/api/users/me/google/challenge");
    expect(challenge[1]).toMatchObject({ method: "POST", body: undefined, signal });
    expect(new Headers(challenge[1]?.headers).get("Authorization")).toBe("Bearer crm-access-token");

    expect(link[0]).toBe("https://crm.test/api/users/me/google");
    expect(link[1]).toMatchObject({ method: "POST", signal });
    expect(new Headers(link[1]?.headers).get("Authorization")).toBe("Bearer crm-access-token");
    expect(JSON.parse(String(link[1]?.body))).toEqual({ idToken: "google-id-token" });
  });

  it.each([null, {}, { nonce: "not-a-challenge" }])("rejects an invalid challenge response %j", async (response) => {
    const api = { post: vi.fn().mockResolvedValue(response) };
    await expect(createGoogleLinkChallenge(api, signal)).rejects.toThrow("invalid Google challenge");
  });

  it("offers a fresh attempt after an expired challenge and reports link conflicts", () => {
    expect(getGoogleLinkError(new ApiError(400, "Auth.InvalidGoogleCredential"))).toContain("Start a new attempt");
    expect(getGoogleLinkError(new ApiError(409, "Auth.GoogleLinkConflict"))).toContain("already linked");
    expect(getGoogleLinkError(new SessionChangedError())).toContain("Your session changed");
  });
});
