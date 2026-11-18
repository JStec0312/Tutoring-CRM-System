import type { ApiClient } from "@/shared/api/api-client";

import { isGoogleLinkExpired, type GoogleLinkContinuation } from "../models/google-link-continuation";

export function completeGoogleAccountLink(api: ApiClient, continuation: GoogleLinkContinuation) {
  if (isGoogleLinkExpired(continuation)) {
    throw new Error("Google.LinkExpired");
  }

  return api.post<void>(
    "/api/auth/google/link",
    { continuationToken: continuation.continuationToken },
    { auth: true },
  );
}
