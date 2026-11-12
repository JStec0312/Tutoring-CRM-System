import { describe, expect, it, vi } from "vitest";

import { createApiClient } from "@/shared/api/api-client";
import {
  createSessionApi,
  InvalidSessionResponseError,
} from "@/shared/session/session-api";

function futureDate() {
  return new Date(Date.now() + 60 * 60 * 1000).toISOString();
}

function makeResponse(body: unknown) {
  return new Response(JSON.stringify(body), {
    status: 200,
    headers: { "Content-Type": "application/json" },
  });
}

describe("SessionApi", () => {
  it("logs in on native and returns both tokens", async () => {
    const fetcher = vi.fn(
      async (_url: RequestInfo | URL, _init?: RequestInit) =>
        makeResponse({
          accessToken: "access-123",
          expiresAt: futureDate(),
          refreshToken: "refresh-123",
          refreshTokenExpiresAt: futureDate(),
        }),
    );

    const api = createApiClient({
      baseUrl: "https://example.com",
      fetcher,
    });

    const sessionApi = createSessionApi(api, "native");

    const result = await sessionApi.login({
      email: "test@example.com",
      password: "password",
    });

    expect(result.accessToken).toBe("access-123");
    expect(result.refreshToken).toBe("refresh-123");

    expect(fetcher.mock.calls[0][0]).toBe(
      "https://example.com/api/auth/mobile/login",
    );
  });

  it("uses the web refresh endpoint without a JSON token", async () => {
    const fetcher = vi.fn(
      async (_url: RequestInfo | URL, _init?: RequestInit) =>
        makeResponse({
          accessToken: "new-access",
          expiresAt: futureDate(),
        }),
    );

    const api = createApiClient({
      baseUrl: "https://example.com",
      credentials: "include",
      fetcher,
    });

    const sessionApi = createSessionApi(api, "web");

    const result = await sessionApi.refresh(null);

    expect(result.refreshToken).toBeNull();
    expect(fetcher.mock.calls[0][0]).toBe(
      "https://example.com/api/auth/refresh",
    );
    expect(fetcher.mock.calls[0][1]?.credentials).toBe("include");
    expect(fetcher.mock.calls[0][1]?.body).toBeUndefined();
  });

  it("rejects an incomplete native response", async () => {
    const fetcher = vi.fn(
      async (_url: RequestInfo | URL, _init?: RequestInit) =>
        makeResponse({
          accessToken: "access-123",
          expiresAt: futureDate(),
          // Missing refresh token
        }),
    );

    const api = createApiClient({
      baseUrl: "https://example.com",
      fetcher,
    });

    const sessionApi = createSessionApi(api, "native");

    await expect(
      sessionApi.login({
        email: "test@example.com",
        password: "password",
      }),
    ).rejects.toBeInstanceOf(InvalidSessionResponseError);
  });
});
