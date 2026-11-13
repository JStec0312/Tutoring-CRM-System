import type { ApiClient } from "@/shared/api/api-client";

export function requestPasswordReset(api: ApiClient, email: string) {
  return api.post("/api/auth/password-reset/request", { email: email.trim() }, { auth: false });
}
