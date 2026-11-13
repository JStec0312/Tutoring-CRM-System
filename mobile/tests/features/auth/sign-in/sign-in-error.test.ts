import { expect, it } from "vitest";

import { getSignInError } from "@/features/auth/sign-in/sign-in-error";
import { ApiError } from "@/shared/api/api-error";

it("shows the email confirmation message for the inactive account code returned by the API", () => {
  expect(getSignInError(new ApiError(403, "Auth.InactiveAccount."))).toContain(
    "potwierdź adres e-mail",
  );
});
