import { ApiError } from "@/shared/api/api-error";

export function getGoogleSignInError(error: unknown): string {
  if (error instanceof ApiError) {
    switch (error.code) {
      case "Auth.InvalidGoogleSignIn":
        return "Your Google sign-in attempt is invalid or expired. Please try again.";
      case "Auth.GoogleVerifiedEmailRequired":
        return "Choose a Google account with a verified email address.";
      case "Auth.InactiveAccount.":
      case "Auth.InactiveAccount":
        return "Your CRM account is inactive. Confirm your email address or contact support.";
      case "Auth.GoogleSignInUnavailable":
        return "Google sign-in is temporarily unavailable. Please try again later.";
      case "Network.Unavailable":
        return "Unable to connect to the server. Please try again.";
    }

    if (error.status === 429) {
      return "Too many sign-in attempts. Please try again shortly.";
    }
  }

  if (error instanceof Error) {
    switch (error.message) {
      case "Google.ClientIdMissing":
      case "Google.IosClientIdMissing":
        return "Google sign-in is not configured for this platform yet.";
      case "Google.PopupBlocked":
        return "Allow popups for this site and try again.";
      case "Session.StorageFailed":
        return "Unable to save your session on this device. Please try again.";
      case "Google.ChallengeExpired":
        return "Your Google sign-in attempt expired. Please try again.";
    }
  }

  return "Unable to sign in with Google. Please try again.";
}
