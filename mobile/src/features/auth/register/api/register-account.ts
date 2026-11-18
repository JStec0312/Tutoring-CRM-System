import type { ApiClient } from "@/shared/api/api-client";
import type { RegistrationRole } from "@/shared/session/registration-role";

export type { RegistrationRole } from "@/shared/session/registration-role";

const endpoints = {
  student: "/api/auth/register/student",
  tutor: "/api/auth/register/tutor",
} as const;

export function registerAccount(
  api: ApiClient,
  role: RegistrationRole,
  { email, password, userName }: { email: string; password: string; userName: string },
) {
  return api.post(endpoints[role], {
    email: email.trim(),
    userName: userName.trim(),
    password,
  }, { auth: false });
}
