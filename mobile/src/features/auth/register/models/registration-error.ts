import { ApiError } from "@/shared/api/api-error";

export function getRegistrationError(error: unknown): string {
  if (error instanceof ApiError) {
    switch (error.code) {
      case "Email.InvalidEmailAddressFormat":
        return "Podaj poprawny adres e-mail.";
      case "Email.AlreadyTaken":
        return "Ten adres e-mail jest już zajęty.";
      case "Username.AlreadyTaken":
        return "Ta nazwa użytkownika jest już zajęta.";
      case "Password.PolicyViolation":
        return "Hasło nie spełnia wymagań.";
      case "Network.Unavailable":
        return "Nie można połączyć się z serwerem.";
    }

    if (error.status === 429) {
      return "Zbyt wiele prób rejestracji. Spróbuj ponownie za chwilę.";
    }
  }

  return "Nie udało się utworzyć konta. Spróbuj ponownie.";
}
