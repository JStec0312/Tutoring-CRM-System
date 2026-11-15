import { ApiError } from "@/shared/api/api-error";
import { SessionChangedError } from "@/shared/session/session-manager";

export function getGoogleLinkError(error: unknown): string {
  if (error instanceof SessionChangedError) {
    return "Your session changed. Start again from your account settings.";
  }

  if (error instanceof ApiError) {
    switch (error.code) {
      case "Auth.IncorrectCurrentPassword":
        return "Your current CRM password is incorrect.";
      case "Auth.InvalidGoogleCredential":
        return "This Google attempt expired or was already used. Start a new attempt.";
      case "Auth.GoogleLinkConflict":
        return "Your CRM account or the selected Google account is already linked.";
      case "Auth.GoogleUnavailable":
        return "Google account verification is temporarily unavailable. Try again later.";
      case "Auth.InactiveAccount.":
        return "Your CRM account must be active to connect Google.";
      case "Network.Unavailable":
        return "Could not reach the CRM server. Check your connection and try again.";
    }
  }

  return "Could not connect your Google account. Try again.";
}
