# Authentication

The API uses JWT access tokens and rotating refresh tokens. Access tokens are returned in response bodies. Raw refresh tokens are sent only in a cookie; only their SHA-256 hashes are persisted.

## Endpoints

| Endpoint | Authentication | Result |
| --- | --- | --- |
| `POST /api/auth/register/student` | Anonymous | Creates an active `UserAccount` with the Student role and a `Student`; returns `201 Created`. |
| `POST /api/auth/register/tutor` | Anonymous | Creates an active `UserAccount` with the Tutor role and a `Tutor`; returns `201 Created`. |
| `POST /api/auth/login` | Anonymous | Returns an access token in the body and sets the refresh-token cookie. |
| `POST /api/auth/refresh` | Refresh-token cookie | Rotates the refresh token, returns a new access token, and replaces the cookie. |
| `POST /api/auth/sign-out` | Anonymous | Revokes the current refresh token when present and deletes its cookie. |
| `POST /api/auth/sign-out-all` | Bearer JWT | Revokes all active refresh tokens for the authenticated account and deletes the cookie. |

## Registration

`RegisterStudent` and `RegisterTutor` are separate vertical slices. Each handler:

1. creates and validates the email value object;
2. checks email and username uniqueness;
3. when supplied, creates the phone-number value object and checks its uniqueness;
4. applies the configured `PasswordPolicyOptions` and hashes the password with BCrypt;
5. creates a `UserAccount`, assigns the relevant role, and creates a `Student` or `Tutor`;
6. saves the account and role-specific entity with EF Core.

## Login and JWT

`LoginHandler` looks up the account and its role assignments by email using a no-tracking query, verifies the BCrypt hash, and requires an active account. It then creates an access token and a refresh token in a new token family, persists the refresh-token record, and returns both values to the controller. `LoginController` exposes only the access token and its expiry in the JSON body and writes the raw refresh token to the configured cookie.

JWT behavior is configured through `JwtOptions`. Tokens are signed with HMAC SHA-256 and contain:

- `sub`: `UserAccountId`;
- `email`: account email;
- `jti`: a new token identifier;
- one `role` claim per assigned role.

Bearer validation checks issuer, audience, lifetime, and signing key. `ClockSkew` is zero, `role` is the role claim type, and `sub` is the name claim type. Secrets are configuration values and must not be copied into documentation or source control.

## Refresh tokens and rotation

`IRefreshTokenGenerator` creates 64 cryptographically random bytes and Base64-encodes them. The raw value is returned to the client, while a SHA-256 hash is stored in `RefreshTokens` with `UserAccountId`, `FamilyId`, creation and expiry timestamps, optional revocation/replacement identifiers, and request IP/user-agent metadata.

Cookie properties come from `RefreshTokenOptions`: name, `HttpOnly`, `Secure`, `SameSite`, `Path`, and expiration.

For `POST /api/auth/refresh`, the controller reads the cookie and the handler:

1. hashes the raw token and loads its database record;
2. rejects an unknown token;
3. if the record is revoked or expired, revokes every still-active token in that family and rejects the request;
4. loads the account and roles and rejects a missing or inactive account;
5. creates a new access token and a new refresh token with the same `FamilyId`;
6. revokes the old token with `ReplacedByTokenId` set to the new token ID, adds the new record, and saves both changes;
7. returns the new access token and replaces the refresh-token cookie.

Step 3 is refresh-token reuse detection. In the current implementation, both revoked and expired presented tokens trigger revocation of active tokens in the same family.

## Sign-out

`POST /api/auth/sign-out` hashes the cookie value, finds the stored token, revokes it when it is active, and deletes the cookie. Missing, unknown, or already revoked tokens are treated as successful, so the operation is idempotent.

`POST /api/auth/sign-out-all` requires JWT authentication. The controller parses `UserAccountId` from the `sub` claim; the handler revokes every refresh token for that account whose `RevokedAtUtc` is null, then the controller deletes the cookie.

## Diagrams

- [Login sequence](diagrams/authentication_login.puml)
- [Refresh sequence](diagrams/authentication_refresh.puml)
