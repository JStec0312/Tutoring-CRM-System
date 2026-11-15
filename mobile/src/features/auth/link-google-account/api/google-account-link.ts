import type { ApiClient } from "@/shared/api/api-client";

export async function createGoogleLinkChallenge(
  api: Pick<ApiClient, "post">,
  signal: AbortSignal,
): Promise<string> {
  const response = await api.post<{ nonce?: unknown } | null>(
    "/api/users/me/google/challenge", undefined, { signal },
  );
  if (typeof response?.nonce !== "string" || !/^[0-9A-F]{64}$/.test(response.nonce)) {
    throw new Error("The API returned an invalid Google challenge");
  }

  return response.nonce;
}

export async function linkGoogleAccount(
  api: Pick<ApiClient, "post">,
  idToken: string,
  currentPassword: string,
  signal: AbortSignal,
): Promise<void> {
  await api.post("/api/users/me/google", { idToken, currentPassword }, { signal });
}
