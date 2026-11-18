import { AuthRequest } from "expo-auth-session";

import type { ApiClient } from "@/shared/api/api-client";
import { createGoogleSignInChallenge } from "../api/create-google-sign-in-challenge";
import { getGoogleWebClientId } from "../models/google-sign-in-config";

export async function requestGoogleIdToken(api: ApiClient): Promise<string | null> {
  const clientId = getGoogleWebClientId();
  const windowName = "crm-google-sign-in";
  // Open during the click, before awaiting HTTP, to avoid popup blockers.
  const popup = window.open("about:blank", windowName, "popup,width=500,height=650");

  if (!popup) {
    throw new Error("Google.PopupBlocked");
  }

  try {
    const challenge = await createGoogleSignInChallenge(api);

    if (popup.closed) {
      return null;
    }

    const request = new AuthRequest({
      clientId,
      redirectUri: new URL("/google-callback", window.location.origin).toString(),
      responseType: "id_token token",
      scopes: ["openid", "email", "profile"],
      usePKCE: false,
      extraParams: { nonce: challenge.nonce, prompt: "select_account" },
    });
    const result = await request.promptAsync(
      { authorizationEndpoint: "https://accounts.google.com/o/oauth2/v2/auth" },
      { windowName },
    );

    if (result.type === "cancel" || result.type === "dismiss") {
      return null;
    }

    if (result.type === "error" && result.error?.code === "access_denied") {
      return null;
    }

    if (result.type !== "success" || !result.params.id_token) {
      throw new Error("Google.MissingIdToken");
    }

    if (Date.parse(challenge.expiresAtUtc) <= Date.now()) {
      throw new Error("Google.ChallengeExpired");
    }

    return result.params.id_token;
  } finally {
    popup.close();
  }
}
