import type { ApiClient } from "@/shared/api/api-client";

import { isGoogleContinuationExpired, type GoogleContinuation } from "../../sign-in/models/google-continuation";

export function completeGoogleAccountLink(api: ApiClient, continuation: GoogleContinuation) {
  if (continuation.status !== "linkRequired" || isGoogleContinuationExpired(continuation)) {
    throw new Error("Google.LinkExpired");
  }

  return api.post<void>(
    "/api/auth/google/link",
    { continuationToken: continuation.continuationToken },
    { auth: true },
  );
}
