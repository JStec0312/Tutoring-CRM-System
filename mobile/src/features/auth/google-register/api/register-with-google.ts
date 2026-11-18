import type { ApiClient } from "@/shared/api/api-client";
import type { RegistrationRole } from "@/shared/session/registration-role";
import { InvalidSessionResponseError, parseSession } from "@/shared/session/session-api";
import { isGoogleContinuationExpired, type GoogleContinuation } from "../../sign-in/models/google-continuation";

export async function registerWithGoogle(
  api: ApiClient,
  continuation: GoogleContinuation,
  role: RegistrationRole,
  userName: string,
  useNativeSession: boolean,
) {
  if (continuation.status !== "registrationRequired" || isGoogleContinuationExpired(continuation)) {
    throw new Error("Google.RegistrationExpired");
  }

  let apiRole = "Tutor";
  if (role === "student") {
    apiRole = "Student";
  }

  const response = await api.post<unknown>("/api/auth/google/register", {
    continuationToken: continuation.continuationToken,
    role: apiRole,
    userName: userName.trim(),
    useNativeSession,
  }, { auth: false });

  if (typeof response !== "object" || response === null || !("status" in response) || response.status !== "signedIn") {
    throw new InvalidSessionResponseError();
  }

  return parseSession(response, useNativeSession);
}
