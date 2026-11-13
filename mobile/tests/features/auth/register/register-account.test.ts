import { describe, expect, it, vi } from "vitest";

import { registerAccount } from "@/features/auth/register/api/register-account";
import { getRegistrationError } from "@/features/auth/register/models/registration-error";
import { createApiClient } from "@/shared/api/api-client";

describe("registerAccount", () => {
  it.each(["student", "tutor"] as const)("registers a %s without a session", async (role) => {
    const fetcher = vi.fn(async (_url: RequestInfo | URL, _options?: RequestInit) =>
      new Response(JSON.stringify({ userId: "new-user" }), { status: 201 }),
    );
    const api = createApiClient({ baseUrl: "https://example.com", fetcher });

    await registerAccount(api, role, {
      email: " user@example.com ",
      userName: " user ",
      password: " Secret123! ",
    });

    const [url, options] = fetcher.mock.calls[0];
    expect(url).toBe(`https://example.com/api/auth/register/${role}`);
    expect(options?.method).toBe("POST");
    expect(new Headers(options?.headers).has("Authorization")).toBe(false);
    expect(JSON.parse(String(options?.body))).toEqual({
      email: "user@example.com",
      userName: "user",
      password: " Secret123! ",
    });
  });

  it("translates a registration conflict returned by the API", async () => {
    const api = createApiClient({
      baseUrl: "https://example.com",
      fetcher: async () => new Response(
        JSON.stringify({ code: "Email.AlreadyTaken" }),
        { status: 409 },
      ),
    });

    const result = registerAccount(api, "student", {
      email: "taken@example.com",
      userName: "user",
      password: "Secret123!",
    }).catch(getRegistrationError);

    await expect(result).resolves.toBe("Ten adres e-mail jest już zajęty.");
  });
});
