# Mobile authentication

## Runtime wiring

- [`src/app/_layout.tsx`](../../../mobile/src/app/_layout.tsx) creates one session manager per root mount. It injects a plain HTTP client into the auth API and exposes a separate bearer-authenticated client via `ApiProvider`. Both clients use `EXPO_PUBLIC_API_URL`.
- [`AuthProvider`](../../../mobile/src/shared/session/AuthProvider.tsx) starts `restoreSession()` on mount. `useAuth()` subscribes to manager snapshots via `useSyncExternalStore` and exposes `signIn` and `signOut`.
- [`SignInScreen`](../../../mobile/src/features/auth/sign-in/SignInScreen.tsx) trims email, checks both fields, prevents duplicate submissions and maps API error codes to UI messages.

## Account registration

[RegisterScreen](../../../mobile/src/features/auth/register/components/RegisterScreen.tsx) collects the account type, email, username and password. It sends an anonymous request through the shared API client, handles conflicts and password policy errors, and prevents duplicate submission. A successful `201 Created` shows instructions for confirming the address by email. The account stays pending until the link is confirmed.

| Account type | Method and endpoint | JSON body |
| --- | --- | --- |
| Student | `POST /api/auth/register/student` | `{ email, userName, password }` |
| Tutor | `POST /api/auth/register/tutor` | `{ email, userName, password }` |

See [US-001](../../technical_docs/backend_docs/user_stories/us_001/README.md) for optional profile fields and the verification email flow.

## Password recovery

From `/sign-in`, open `/forgot-password` and submit the account email. The app sends `POST /api/auth/password-reset/request` with `{ email }` and shows the same confirmation for any `204 No Content` response. The emailed link opens `/reset-password?token=...`, where the app sends `POST /api/auth/password-reset` with `{ token, newPassword }`. Both requests are anonymous. An invalid or expired link offers a new request.

The backend reads the frontend reset URL from `PasswordReset:ResetUrl` in `appsettings.json`. The local default is `http://localhost:8081/reset-password`. For a deployed frontend, override it with `PasswordReset__ResetUrl` set to the public HTTPS reset page. See [US-004](../../technical_docs/backend_docs/user_stories/us_004/README.md) for token handling.

## HTTP contract

All operations use `POST` with `auth: false` to skip bearer injection.

| Operation | Web | Android and iOS |
| --- | --- | --- |
| Sign in | `/api/auth/login` with `{ email, password }` | `/api/auth/mobile/login` with `{ email, password }` |
| Refresh | `/api/auth/refresh` with a cookie and no JSON body | `/api/auth/mobile/refresh` with `{ refreshToken }` |
| Sign out | `/api/auth/sign-out` with a cookie and no JSON body | `/api/auth/mobile/sign-out` with `{ refreshToken }` |

[`session-api.ts`](../../../mobile/src/shared/session/session-api.ts) validates `accessToken` and `expiresAt` from login and refresh responses. Native responses also require `refreshToken` and `refreshTokenExpiresAt`. Expiry values must be valid future timestamps.

The authenticated client attaches a bearer token and captures the session generation. It checks the generation after obtaining the token, receiving headers, reading the body and refreshing. An obsolete request throws `SessionChangedError`. A protected `401` passes the failed bearer token to the manager, which shares any in-flight refresh or returns an already rotated token. The client retries once with the same method, JSON body and abort signal. A second `401` returns `ApiError`. A `403`, network failure, cancellation or `auth: false` request skips this recovery path.

## Session lifecycle

- The [session manager](../../../mobile/src/shared/session/session-manager.ts) keeps the access token in memory. Native stores the refresh token in Expo SecureStore and saves each rotated token before publishing `authenticated`. Web uses `credentials: "include"` and the backend's HttpOnly cookie.
- Restore starts in `restoring`. Native without a stored refresh token, or a rejected refresh with `401` or an invalid session payload, moves to `anonymous`. Other restore failures publish `restore-error`, which exposes a retry action.
- `getValidAccessToken()` refreshes on demand when the access token has 30 seconds or less remaining. The manager shares in-flight restore and refresh promises, serializes credential mutations and uses a session generation counter to prevent stale work from restoring a cleared session. On web, mutations also use an exclusive Web Lock shared by tabs on the same origin.
- Sign-out clears in-memory auth immediately. The queued operation persists a pending marker, requests server revocation and clears stored credentials and the marker on success. Native restore and the next sign-in retry an interrupted sign-out if the marker is set.
- Web tabs use a [storage event](../../../mobile/src/shared/session/session-sync.web.ts) containing only an event ID and type. Sign-in publishes after the backend sets the cookie while the originating tab holds the Web Lock. Other tabs discard their in-memory session and restore from that cookie. Sign-out publishes immediately, invalidating any earlier restore and clearing other tabs without another sign-out request. The manager also reads the latest event under the Web Lock because event delivery is asynchronous.

## Route guards

| Session state | Available routes |
| --- | --- |
| `restoring`, `restore-error` | `/session-status` |
| `anonymous` | `/sign-in`, `/register` |
| `authenticated` | `/` routes a Tutor to `/tutor` and a Student to `/student` |
| Any session state | `/forgot-password`, `/reset-password` |

The session manager exposes the `role` claim from the backend access token through `useAuth().roles`. It reads the claim on sign-in, restore and refresh, and clears roles on session reset. If neither portal role or both portal roles are present, `/` shows a message and a sign-out action. The Parent screen and route are reserved until the backend supports that role. Each portal has its own Expo Router guard.

The root guards send protected routes to `/session-status` while restoring. After restoration, the fallback is `/sign-in` for anonymous users and `/` for authenticated users. Password recovery routes are public, so opening `/reset-password?token=...` directly retains the token while the session restores. The `(auth)` directory contributes no URL segment.

## Local verification

Set `EXPO_PUBLIC_API_URL` to the backend origin in `mobile/.env.local`. From `mobile`, run:

```sh
npm ci
npx tsc --noEmit
npx vitest run
npx expo start --web
```

The Vitest suite covers platform-specific session API requests, native response validation, stale responses, concurrent `401` recovery, cancellation and sign-in/sign-out across simulated tabs. See the [target mobile architecture](../architecture/README.md) for the broader design.
