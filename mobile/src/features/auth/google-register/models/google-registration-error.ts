import { ApiError } from "@/shared/api/api-error";

export function getGoogleRegistrationError(error: unknown): string {
  if (error instanceof Error && error.message === "Google.RegistrationExpired") {
    return "This registration attempt has expired. Start Google sign-in again.";
  }

  if (error instanceof ApiError) {
    switch (error.code) {
      case "Auth.InvalidGoogleRegistrationContinuation":
        return "This registration attempt is no longer available. Start Google sign-in again.";
      case "Auth.GoogleRegistrationConflict":
        return "An account with these Google details already exists. Start Google sign-in again.";
      case "Username.AlreadyTaken":
        return "This username is already taken. Choose another one.";
      case "Network.Unavailable":
        return "Unable to connect to the server. Please try again.";
    }

    if (error.status === 429) {
      return "Too many registration attempts. Please try again shortly.";
    }
  }

  return "Unable to create your account. Please try again.";
}
