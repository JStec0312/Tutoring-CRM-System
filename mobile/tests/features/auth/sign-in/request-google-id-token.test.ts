import { afterEach, beforeEach, expect, it, vi } from "vitest";

const { configure, checkPlayServices, presentExplicitSignIn, platform } = vi.hoisted(() => ({
  configure: vi.fn(),
  checkPlayServices: vi.fn(),
  presentExplicitSignIn: vi.fn(),
  platform: { OS: "android" },
}));

vi.mock("react-native", () => ({ Platform: platform }));
vi.mock("react-native-nitro-google-signin", () => ({
  GoogleOneTapSignIn: { configure, checkPlayServices, presentExplicitSignIn },
  isErrorWithCode: (error: unknown) => typeof error === "object" && error !== null && "code" in error,
  statusCodes: { SIGN_IN_CANCELLED: "SIGN_IN_CANCELLED" },
}));

import { requestGoogleIdToken } from "@/features/auth/sign-in/services/request-google-id-token";
import { createApiClient } from "@/shared/api/api-client";

const nonce = "b".repeat(64);
const api = createApiClient({
  baseUrl: "https://api.example.com",
  fetcher: async () => new Response(JSON.stringify({
    nonce,
    expiresAtUtc: new Date(Date.now() + 300_000).toISOString(),
  })),
});

beforeEach(() => {
  vi.clearAllMocks();
  platform.OS = "android";
  vi.stubEnv("EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID", "web-client-id");
  vi.stubEnv("EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID", "");
  presentExplicitSignIn.mockResolvedValue({ type: "success", data: { idToken: "google-id-token" } });
});

afterEach(() => vi.unstubAllEnvs());

it("requests a fresh native token with the Web client ID and server nonce", async () => {
  await expect(requestGoogleIdToken(api)).resolves.toBe("google-id-token");
  expect(configure).toHaveBeenCalledWith({ webClientId: "web-client-id", iosClientId: "", nonce });
  expect(checkPlayServices).toHaveBeenCalled();
  expect(presentExplicitSignIn).toHaveBeenCalledOnce();
});

it("treats a dismissed native account picker as cancellation", async () => {
  presentExplicitSignIn.mockResolvedValue({ type: "cancelled", data: null });
  await expect(requestGoogleIdToken(api)).resolves.toBeNull();
});

it("treats the SDK cancellation error as cancellation", async () => {
  presentExplicitSignIn.mockRejectedValueOnce({ code: "SIGN_IN_CANCELLED" });
  await expect(requestGoogleIdToken(api)).resolves.toBeNull();
});

it("rejects a successful response without an ID token", async () => {
  presentExplicitSignIn.mockResolvedValue({ type: "success", data: null });
  await expect(requestGoogleIdToken(api)).rejects.toThrow("Google.MissingIdToken");
});

it("reports missing iOS configuration before attempting native sign-in", async () => {
  platform.OS = "ios";
  await expect(requestGoogleIdToken(api)).rejects.toThrow("Google.IosClientIdMissing");
  expect(presentExplicitSignIn).not.toHaveBeenCalled();
});

it("reports missing Web client configuration", async () => {
  vi.stubEnv("EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID", "");
  await expect(requestGoogleIdToken(api)).rejects.toThrow("Google.ClientIdMissing");
});
