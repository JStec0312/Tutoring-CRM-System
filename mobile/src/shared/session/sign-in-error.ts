import { ApiError } from "@/shared/api/api-error";

export function getSignInError(error: unknown): string {
  if (error instanceof ApiError) {
    switch (error.code) {
      case "Auth.GoogleAccountHasNoPassword":
        return "This account uses Google sign-in and has no password. Sign in with Google.";
      case "Auth.InvalidCredentials":
        return "Invalid email address or password.";
      case "Auth.InactiveAccount.":
      case "Auth.InactiveAccount":
        return "Your account is inactive. If you just created it, confirm your email address.";
      case "Network.Unavailable":
        return "Unable to connect to the server.";
    }

    if (error.status === 429) {
      return "Too many sign-in attempts. Please try again shortly.";
    }
  }

  return "Unable to sign in. Please try again.";
}
