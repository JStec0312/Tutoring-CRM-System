import { expect, it } from "vitest";

import { getSignInError } from "@/shared/session/sign-in-error";
import { ApiError } from "@/shared/api/api-error";

it("shows the email confirmation message for the inactive account code returned by the API", () => {
  expect(getSignInError(new ApiError(403, "Auth.InactiveAccount."))).toContain(
    "confirm your email address",
  );
});
