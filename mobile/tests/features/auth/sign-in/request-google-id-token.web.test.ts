import { afterEach, beforeEach, expect, it, vi } from "vitest";

const { promptAsync, requestConfig } = vi.hoisted(() => ({
  promptAsync: vi.fn(),
  requestConfig: vi.fn(),
}));

vi.mock("expo-auth-session", () => ({
  AuthRequest: class {
    constructor(config: unknown) { requestConfig(config); }
    promptAsync = promptAsync;
  },
}));

import { requestGoogleIdToken } from "@/features/auth/sign-in/services/request-google-id-token.web";
import { createApiClient } from "@/shared/api/api-client";

const popup = { closed: false, close: vi.fn() };
const open = vi.fn(() => popup);
const nonce = "a".repeat(64);
const fetcher = vi.fn(async () => new Response(JSON.stringify({
  nonce,
  expiresAtUtc: new Date(Date.now() + 300_000).toISOString(),
})));
const api = createApiClient({ baseUrl: "https://api.example.com", fetcher });

beforeEach(() => {
  vi.clearAllMocks();
  vi.stubEnv("EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID", "web-client-id");
  vi.stubGlobal("window", { open, location: { origin: "https://crm.example.com" } });
  popup.closed = false;
  open.mockReturnValue(popup);
  promptAsync.mockResolvedValue({ type: "success", params: { id_token: "google-id-token" } });
});

afterEach(() => {
  vi.unstubAllEnvs();
  vi.unstubAllGlobals();
});

it("opens the popup before HTTP and passes the backend nonce to Google", async () => {
  await expect(requestGoogleIdToken(api)).resolves.toBe("google-id-token");

  expect(open.mock.invocationCallOrder[0]).toBeLessThan(fetcher.mock.invocationCallOrder[0]);
  expect(requestConfig).toHaveBeenCalledWith(expect.objectContaining({
    clientId: "web-client-id",
    redirectUri: "https://crm.example.com/google-callback",
    responseType: "id_token token",
    extraParams: { nonce, prompt: "select_account" },
  }));
  expect(popup.close).toHaveBeenCalled();
});

it.each(["cancel", "dismiss"])("treats %s as cancellation", async (type) => {
  promptAsync.mockResolvedValue({ type });
  await expect(requestGoogleIdToken(api)).resolves.toBeNull();
  expect(popup.close).toHaveBeenCalled();
});

it("treats declined consent as cancellation", async () => {
  promptAsync.mockResolvedValue({ type: "error", error: { code: "access_denied" } });
  await expect(requestGoogleIdToken(api)).resolves.toBeNull();
});

it("does not start Google if the user closes the popup while loading the challenge", async () => {
  popup.closed = true;
  await expect(requestGoogleIdToken(api)).resolves.toBeNull();
  expect(promptAsync).not.toHaveBeenCalled();
});

it("reports a blocked popup without requesting a challenge", async () => {
  open.mockReturnValueOnce(null as never);
  await expect(requestGoogleIdToken(api)).rejects.toThrow("Google.PopupBlocked");
  expect(fetcher).not.toHaveBeenCalled();
});

it("rejects an OAuth failure instead of accepting a token", async () => {
  promptAsync.mockResolvedValue({ type: "error", error: { code: "state_mismatch" } });
  await expect(requestGoogleIdToken(api)).rejects.toThrow();
  expect(popup.close).toHaveBeenCalled();
});

it("closes the popup when the challenge request fails", async () => {
  const failingApi = createApiClient({
    baseUrl: "https://api.example.com",
    fetcher: async () => new Response("", { status: 503 }),
  });
  await expect(requestGoogleIdToken(failingApi)).rejects.toMatchObject({ status: 503 });
  expect(popup.close).toHaveBeenCalled();
  expect(promptAsync).not.toHaveBeenCalled();
});

it("rejects a token if the backend challenge expired while Google was open", async () => {
  const expiredApi = createApiClient({
    baseUrl: "https://api.example.com",
    fetcher: async () => new Response(JSON.stringify({ nonce, expiresAtUtc: "2020-01-01T00:00:00Z" })),
  });
  await expect(requestGoogleIdToken(expiredApi)).rejects.toThrow("Google.ChallengeExpired");
});
