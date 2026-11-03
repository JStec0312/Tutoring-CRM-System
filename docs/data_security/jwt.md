# JWT authentication and authorization

A JSON Web Token (JWT) is a signed set of claims that a recipient can validate. The API uses it as a bearer access token.

## Flow

- `POST /api/auth/login` checks the password and active account status. It returns an access token and expiry in the response body and sets a refresh-token cookie.
- The access token is signed with HMAC-SHA256. It contains `sub` (account ID), `email`, `jti` and one `role` claim per assigned role, plus issuer, audience and time claims. The configured lifetime is 30 minutes.
- Bearer authentication validates the signing key, issuer, audience and lifetime with zero clock skew. Controllers use `[Authorize]` and, where specified, roles such as `Tutor` or `Student`; resource ownership is checked separately by handlers.
- The refresh token is a random opaque value, not a JWT. Its SHA-256 hash and metadata are stored in `RefreshTokens`; the raw value is sent in the `refresh_token` cookie. Defaults are 30 days, HttpOnly, SameSite=Strict, Secure and path `/api/auth/`; Development overrides Secure to false.
- `POST /api/auth/refresh` reads the cookie, checks the token and active account, issues a new access token, revokes the old refresh token and rotates it within the same family. Reuse of a revoked or expired token revokes the remaining unrevoked family tokens.
- `POST /api/auth/sign-out` revokes the supplied cookie token; authenticated `POST /api/auth/sign-out-all` revokes the account's unrevoked refresh tokens. Password reset also revokes them. Existing access tokens remain usable until expiry: no access-token revocation check is configured.

## Implementation

- [Authentication configuration](../../TutoringManagementSystem/Tutoring.Api/Configuration/AuthenticationExtensions.cs) and [settings](../../TutoringManagementSystem/Tutoring.Api/appsettings.json).
- [JWT generator](../../TutoringManagementSystem/Tutoring.Infrastructure/Authentication/JwtTokenGenerator.cs), [refresh-token generator](../../TutoringManagementSystem/Tutoring.Infrastructure/Authentication/RefreshTokenGenerator.cs) and [database mapping](../../TutoringManagementSystem/Tutoring.Infrastructure/Persistence/Configurations/RefreshTokenConfigurations.cs).
- [Login](../../TutoringManagementSystem/Tutoring.Api/Features/Auth/Login), [refresh](../../TutoringManagementSystem/Tutoring.Api/Features/Auth/RefreshToken), [sign out](../../TutoringManagementSystem/Tutoring.Api/Features/Auth/SignOut), [sign out all](../../TutoringManagementSystem/Tutoring.Api/Features/Auth/SignOutAll) and [password reset](../../TutoringManagementSystem/Tutoring.Api/Features/Auth/ResetPassword).

## Verification

With the API running, obtain access tokens through login for active Tutor and Student accounts. For the Tutor-only endpoint below, substitute each token locally; do not commit it. A valid Tutor with a valid account identity gets `200`, no token gets `401`, and a valid Student token without the Tutor role gets `403`.

```sh
curl -i http://localhost:5106/api/tutors/me/students -H "Authorization: Bearer <TUTOR_ACCESS_TOKEN>"
curl -i http://localhost:5106/api/tutors/me/students
curl -i http://localhost:5106/api/tutors/me/students -H "Authorization: Bearer <STUDENT_ACCESS_TOKEN>"
```
