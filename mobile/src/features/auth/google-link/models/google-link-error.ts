import { ApiError } from "@/shared/api/api-error";

export function getGoogleLinkError(error: unknown): string {
  if (error instanceof Error && error.message === "Google.LinkExpired") {
    return "This linking attempt has expired. Start Google sign-in again.";
  }

  if (error instanceof ApiError) {
    switch (error.code) {
      case "Auth.InvalidGoogleLinkContinuation":
        return "Sign in to the CRM account with the same email address as your Google account. If this attempt has expired, start Google sign-in again.";
      case "Auth.GoogleAccountLinkConflict":
        return "The Google account or CRM account is already connected to another account.";
      case "Auth.InactiveAccount.":
      case "Auth.InactiveAccount":
        return "This CRM account is inactive.";
      case "Network.Unavailable":
        return "Unable to connect to the server. Please try again.";
    }

    if (error.status === 401) {
      return "Your CRM session is no longer available. Sign in again to continue.";
    }
  }

  return "Unable to connect your Google account. Please try again.";
}
