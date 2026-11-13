import { ApiError } from "@/shared/api/api-error";

export function getResetPasswordError(error: unknown): { message: string; invalidLink?: true } {
  if (error instanceof ApiError) {
    if (error.code === "Auth.InvalidPasswordResetToken") {
      return { message: "Ten link jest nieważny lub wygasł.", invalidLink: true };
    }
    if (error.code === "Password.PolicyViolation") {
      return { message: "Hasło nie spełnia wymagań." };
    }
    if (error.code === "Network.Unavailable") {
      return { message: "Nie można połączyć się z serwerem." };
    }
    if (error.status === 429) {
      return { message: "Zbyt wiele prób. Spróbuj ponownie za chwilę." };
    }
  }

  return { message: "Nie udało się zmienić hasła. Spróbuj ponownie." };
}
