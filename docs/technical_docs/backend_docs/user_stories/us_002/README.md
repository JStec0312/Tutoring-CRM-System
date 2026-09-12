# US-002 — Sign in and sign out

> As a user, I want to sign in and sign out so that I can securely access my account.

## Scope

An active user logs in with email/password and receives a JWT access token plus an `HttpOnly` refresh-token cookie. The refresh endpoint rotates the token and issues a new access token. Sign-out revokes the current refresh token (or all of a user's active tokens) and clears the cookie.

## API

| Method | Endpoint | Auth | Result |
| --- | --- | --- | --- |
| POST | `/api/auth/login` | Anonymous | Returns an access token and sets the refresh-token cookie for an active account. |
| POST | `/api/auth/refresh` | Refresh-token cookie | Rotates the refresh token, returns a new access token, replaces the cookie. |
| POST | `/api/auth/sign-out` | Anonymous (cookie-based) | Revokes the presented refresh token when active and deletes the cookie. |
| POST | `/api/auth/sign-out-all` | JWT | Revokes all active refresh tokens for the authenticated account and deletes the cookie. |

## Implementation

- Login rejects unknown emails and wrong passwords with `401 InvalidCredentials`, and a valid but `PendingActivation`/inactive account with `403 InactiveAccount`.
- On success, `IJwtTokenGenerator` issues a signed JWT (`sub`, `email`, `jti`, role claims) and `IRefreshTokenGenerator` creates a raw token plus its SHA-256 hash, stored with a new refresh-token family ID.
- Refresh tokens form a rotation family: a valid refresh call revokes the old token (`ReplacedByTokenId`) and issues a new one in the same family; a revoked/expired token being reused revokes the rest of the family (reuse detection) before rejecting with `401`.
- Refresh rejects a missing cookie, unknown token, or an inactive/missing account.
- Sign-out is idempotent for a missing, unknown, or already-revoked cookie token; sign-out-all revokes every active refresh token for the authenticated account.
- The raw refresh token is only ever sent through the configured `HttpOnly` cookie; only its hash is persisted.

## Diagram

![Login](diagrams/login.svg)

![Refresh token rotation](diagrams/refresh_token.svg)

## Tests

`Tutoring.IntegrationTests`: login with valid/invalid credentials and inactive accounts, refresh rotation/reuse detection/invalid-token rejection, and sign-out/sign-out-all cookie and revocation behavior.
