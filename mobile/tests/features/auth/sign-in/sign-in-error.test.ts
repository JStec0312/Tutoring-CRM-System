import { expect, it } from "vitest";

import { getSignInError } from "@/shared/session/sign-in-error";
import { ApiError } from "@/shared/api/api-error";

it("shows the email confirmation message for the inactive account code returned by the API", () => {
  expect(getSignInError(new ApiError(403, "Auth.InactiveAccount."))).toContain(
    "confirm your email address",
  );
});

it("names Google when the account supports only Google sign-in", () => {
  expect(getSignInError(new ApiError(409, "Auth.GoogleAccountHasNoPassword"))).toBe(
    "This account uses Google sign-in and has no password. Sign in with Google.",
  );
});

it("keeps the generic credentials message for accounts that have a password", () => {
  expect(getSignInError(new ApiError(401, "Auth.InvalidCredentials"))).toBe(
    "Invalid email address or password.",
  );
});
