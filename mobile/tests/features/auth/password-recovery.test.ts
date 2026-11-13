import { describe, expect, it, vi } from "vitest";

import { requestPasswordReset } from "@/features/auth/forgot-password/api/request-password-reset";
import { resetPassword } from "@/features/auth/reset-password/api/reset-password";
import { getResetPasswordError } from "@/features/auth/reset-password/models/reset-password-error";
import { createApiClient } from "@/shared/api/api-client";

describe("password recovery", () => {
  it.each([
    {
      submit: (api: ReturnType<typeof createApiClient>) =>
        requestPasswordReset(api, " user@example.com "),
      path: "/api/auth/password-reset/request",
      body: { email: "user@example.com" },
    },
    {
      submit: (api: ReturnType<typeof createApiClient>) =>
        resetPassword(api, "token-value", " NewPassword123! "),
      path: "/api/auth/password-reset",
      body: { token: "token-value", newPassword: " NewPassword123! " },
    },
  ])("posts to $path without a session", async ({ submit, path, body }) => {
    const fetcher = vi.fn(async (_url: RequestInfo | URL, _options?: RequestInit) =>
      new Response(null, { status: 204 }),
    );
    const api = createApiClient({ baseUrl: "https://example.com", fetcher });

    await expect(submit(api)).resolves.toBeUndefined();

    const [url, options] = fetcher.mock.calls[0];
    expect(url).toBe(`https://example.com${path}`);
    expect(options?.method).toBe("POST");
    expect(new Headers(options?.headers).has("Authorization")).toBe(false);
    expect(JSON.parse(String(options?.body))).toEqual(body);
  });

  it("prompts for a new link when the token is rejected", async () => {
    const api = createApiClient({
      baseUrl: "https://example.com",
      fetcher: async () => new Response(
        JSON.stringify({ code: "Auth.InvalidPasswordResetToken" }),
        { status: 401 },
      ),
    });

    const result = resetPassword(api, "expired-token", "NewPassword123!")
      .catch(getResetPasswordError);

    await expect(result).resolves.toMatchObject({ invalidLink: true });
  });
});
