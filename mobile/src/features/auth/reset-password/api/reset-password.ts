import type { ApiClient } from "@/shared/api/api-client";

export function resetPassword(api: ApiClient, token: string, newPassword: string) {
  return api.post("/api/auth/password-reset", { token, newPassword }, { auth: false });
}
