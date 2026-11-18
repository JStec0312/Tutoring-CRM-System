import type { ApiClient } from "@/shared/api/api-client";

import type { GoogleSignInChallenge } from "../models/google-sign-in-challenge";

export function createGoogleSignInChallenge(api: ApiClient) {
  return api.post<GoogleSignInChallenge>(
    "/api/auth/google/challenge",
    {},
    { auth: false },
  );
}
