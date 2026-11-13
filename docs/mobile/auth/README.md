# Mobile authentication

## Runtime wiring

- [`src/app/_layout.tsx`](../../../mobile/src/app/_layout.tsx) creates one session manager per root mount. It injects a plain HTTP client into the auth API and exposes a separate bearer-authenticated client via `ApiProvider`. Both clients use `EXPO_PUBLIC_API_URL`.
- [`AuthProvider`](../../../mobile/src/shared/session/AuthProvider.tsx) starts `restoreSession()` on mount. `useAuth()` subscribes to manager snapshots via `useSyncExternalStore` and exposes `signIn` and `signOut`.
- [`SignInScreen`](../../../mobile/src/features/auth/sign-in/SignInScreen.tsx) trims email, checks both fields, prevents duplicate submissions and maps API error codes to UI messages. `/register` and `/forgot-password` are placeholder screens.

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
| `anonymous` | `/sign-in`, `/register`, `/forgot-password` |
| `authenticated` | `(app)` with `/` as its index |

The root `Stack.Protected` registers `(auth)/sign-in` first among the anonymous routes. Expo Router uses `/sign-in` as the fallback when the current route becomes unavailable. The `(auth)` directory groups files and contributes no URL segment. Successful sign-in and sign-out change the session snapshot, which updates the route guards.

## Local verification

Set `EXPO_PUBLIC_API_URL` to the backend origin in `mobile/.env.local`. From `mobile`, run:

```sh
npm ci
npx tsc --noEmit
npx vitest run
npx expo start --web
```

The Vitest suite covers platform-specific session API requests, native response validation, stale responses, concurrent `401` recovery, cancellation and sign-in/sign-out across simulated tabs. See the [target mobile architecture](../architecture/README.md) for the broader design.
