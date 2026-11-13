import { ApiError } from "@/shared/api/api-error";

export function getRequestPasswordResetError(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.code === "Email.InvalidEmailAddressFormat") {
      return "Podaj poprawny adres e-mail.";
    }
    if (error.code === "Network.Unavailable") {
      return "Nie można połączyć się z serwerem.";
    }
    if (error.status === 429) {
      return "Zbyt wiele prób. Spróbuj ponownie za chwilę.";
    }
  }

  return "Nie udało się wysłać zgłoszenia. Spróbuj ponownie.";
}
