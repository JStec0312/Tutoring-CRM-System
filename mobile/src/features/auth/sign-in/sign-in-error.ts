import { ApiError } from "@/shared/api/api-error";

export function getSignInError(error: unknown): string {
  if (error instanceof ApiError) {
    switch (error.code) {
      case "Auth.InvalidCredentials":
        return "Nieprawidłowy adres e-mail lub hasło.";
      case "Auth.InactiveAccount.":
      case "Auth.InactiveAccount":
        return "Konto jest nieaktywne. Jeśli dopiero je utworzyłeś, potwierdź adres e-mail.";
      case "Network.Unavailable":
        return "Nie można połączyć się z serwerem.";
    }

    if (error.status === 429) {
      return "Zbyt wiele prób logowania. Spróbuj ponownie za chwilę.";
    }
  }

  return "Nie udało się zalogować. Spróbuj ponownie.";
}
