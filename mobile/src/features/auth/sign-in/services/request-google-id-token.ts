import { Platform } from "react-native";

import type { ApiClient } from "@/shared/api/api-client";
import { createGoogleSignInChallenge } from "../api/create-google-sign-in-challenge";
import { getGoogleWebClientId } from "../models/google-sign-in-config";

export async function requestGoogleIdToken(api: ApiClient): Promise<string | null> {
  const webClientId = getGoogleWebClientId();
  const iosClientId = process.env.EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID;

  if (Platform.OS === "ios" && !iosClientId) {
    throw new Error("Google.IosClientIdMissing");
  }

  // Load on demand so an older development build can still open the sign-in screen.
  const { GoogleOneTapSignIn, isErrorWithCode, statusCodes } =
    await import("react-native-nitro-google-signin");
  const challenge = await createGoogleSignInChallenge(api);

  GoogleOneTapSignIn.configure({ webClientId, iosClientId, nonce: challenge.nonce });

  try {
    // if a user has Google Play Services available, check them before attempting sign-in.
    await GoogleOneTapSignIn.checkPlayServices();
    // Always request a fresh token containing this attempt's nonce.
    // Present the sign-in UI to the user and request a fresh ID token. (google play services)
    const result = await GoogleOneTapSignIn.presentExplicitSignIn();

    if (result.type === "cancelled") {
      return null;
    }

    if (result.type !== "success" || !result.data?.idToken) {
      throw new Error("Google.MissingIdToken");
    }

    if (Date.parse(challenge.expiresAtUtc) <= Date.now()) {
      throw new Error("Google.ChallengeExpired");
    }

    return result.data.idToken;
  } catch (error) {
    if (isErrorWithCode(error) && error.code === statusCodes.SIGN_IN_CANCELLED) {
      return null;
    }

    throw error;
  }
}
