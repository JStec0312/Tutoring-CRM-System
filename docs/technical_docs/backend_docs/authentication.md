# Authentication

The API uses JWT access tokens and rotating refresh tokens. Access tokens are returned in JSON; the raw refresh token is sent only in a configured `HttpOnly` cookie. SHA-256 hashes, not raw refresh tokens, are stored.

## Endpoints

| Endpoint | Authentication | Result |
| --- | --- | --- |
| `POST /api/auth/register/student` | Anonymous | Creates a pending Student account and returns `201 Created`. |
| `POST /api/auth/register/tutor` | Anonymous | Creates a pending Tutor account and returns `201 Created`. |
| `GET /api/auth/email/confirm?token=...` | Anonymous | Confirms the email and activates the account. |
| `POST /api/auth/login` | Anonymous | For an active account, returns an access token and sets the refresh-token cookie. |
| `POST /api/auth/refresh` | Refresh-token cookie | Rotates the refresh token, returns a new access token, and replaces the cookie. |
| `POST /api/auth/sign-out` | Anonymous | Revokes the presented refresh token when active and deletes the cookie. |
| `POST /api/auth/sign-out-all` | JWT | Revokes all active refresh tokens for the authenticated account and deletes the cookie. |

## Registration and email confirmation

Both registration slices validate uniqueness and password policy, create the role-specific entity, and save these records in one EF Core transaction:

1. `UserAccount` starts as `PendingActivation`; an `EmailVerificationToken` stores only a SHA-256 token hash, expiry, and optional use timestamp.
2. The same save writes a `UserRegistered` event to `messaging.OutboxMessages`.
3. `OutboxProcessor` publishes the event to RabbitMQ. `EmailConsumer` routes it through `EmailEventDispatcher`; `UserRegisteredEmailHandler` builds the confirmation link and `SmtpMailer` sends it.
4. The link calls `GET /api/auth/email/confirm`. A valid, unused, unexpired token activates the account and marks the token used.

Unknown, expired, and previously used verification tokens all return `401` with `Auth.InvalidEmailVerificationToken`. A pending account cannot log in: valid credentials return `403` with `Auth.InactiveAccount.` until confirmation succeeds.

## Access and refresh tokens

JWTs are HMAC SHA-256 signed and include `sub` (account ID), `email`, `jti`, and role claims. Validation checks issuer, audience, lifetime, and signing key with zero clock skew.

Refresh tokens form a family. Refresh rotates the presented active token and retains its family ID. An unknown token is rejected; a revoked or expired presented token revokes remaining active tokens in its family before rejection. Refresh also rejects a missing or inactive account. Sign-out is idempotent for a missing, unknown, or already revoked cookie token.

## Diagrams

- [Registration and email confirmation sequence](diagrams/authentication_registration.puml)
- [Login sequence](diagrams/authentication_login.puml)
- [Refresh sequence](diagrams/authentication_refresh.puml)
