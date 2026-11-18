import type { ApiClient } from "@/shared/api/api-client";
import {
  InvalidSessionResponseError,
  parseSession,
  parseUtcDate,
} from "@/shared/session/session-api";

import type { GoogleContinuation } from "../models/google-continuation";
import type { GoogleSignInResponse } from "../models/google-sign-in-response";

export async function signInWithGoogle(
  api: ApiClient,
  idToken: string,
  useNativeSession: boolean,
): Promise<GoogleSignInResponse> {
  const response = await api.post<unknown>(
    "/api/auth/google/sign-in",
    { idToken, useNativeSession },
    { auth: false },
  );

  if (typeof response !== "object" || response === null || Array.isArray(response)) {
    throw new InvalidSessionResponseError();
  }

  const data = response as Record<string, unknown>;

  if (data.status === "signedIn") {
    return { status: "signedIn", session: parseSession(data, useNativeSession) };
  }

  if (data.status === "linkRequired" || data.status === "registrationRequired") {
    if (typeof data.continuationToken !== "string" || data.continuationToken.length !== 64) {
      throw new InvalidSessionResponseError();
    }

    const continuation: GoogleContinuation = {
      status: data.status,
      continuationToken: data.continuationToken,
      continuationExpiresAtUtc: parseUtcDate(data.continuationExpiresAtUtc),
    };

    if (data.status === "linkRequired") {
      if (typeof data.email !== "string" || !data.email.trim() || data.email.length > 320) {
        throw new InvalidSessionResponseError();
      }
      continuation.email = data.email.trim();
    }

    return continuation;
  }

  throw new InvalidSessionResponseError();
}
