# Session and login

`createAuthController()` creates the `session` object and its store. At startup, `runtime.ts` gives the same object to `SessionProvider` and the API client. [Controller](../../../mobile/src/features/auth/models/auth-controller.ts) · [Runtime](../../../mobile/src/bootstrap/runtime.ts)

<a id="3-provider-context-and-hook"></a>

## Startup and screen state

The provider calls `session.restoreSession()` and starts listening for session events from other browser tabs. Screens call `useSession()`, which reads the local store and updates them when it changes. The snapshot contains `{ status, user, error }`. The store keeps the access token separately in memory. [Provider and hook](../../../mobile/src/shared/session/session-context.tsx) · [Store](../../../mobile/src/shared/session/session-store.ts)

| Status | What happens |
|---|---|
| `Restoring` | The app checks whether it can renew the previous session. |
| `Authenticated` | Restoration succeeds and the app opens the workspace. |
| `Anonymous` | A pending logout, missing native refresh token, missing API URL, or rejected refresh leads to the sign-in screen. |
| `RestoreError` | A temporary problem, such as a network failure, leads to a retry screen. |

[Startup and login diagram](diagrams/startup.svg) · [PlantUML source](diagrams/startup.puml)

## Login and token refresh

`signIn()` sends the credentials to the backend. After a successful response, the controller saves the session and updates the store to `Authenticated`. The layout opens the workspace in response to that state change.

Before a protected request or when a signed-in user returns to the app, `ensureAccessToken()` refreshes a missing token or one that expires within 30 seconds. Concurrent callers share one refresh. On `401`, the API client can retry once with the latest token. A second `401` clears the session. [API client](../../../mobile/src/shared/api/api-client.ts)

<a id="7-signing-out-and-pending-logout"></a>

## Logout

`signOut()` clears local state immediately, then attempts to revoke the refresh token on the backend. A saved `LogoutPending` marker blocks automatic restoration if the backend does not confirm logout. Successful logout or explicit sign-in clears the marker.

The auth queue orders login, refresh, and logout. The `generation` counter discards responses from an earlier session, such as a refresh that finishes after logout.

[Logout diagram](diagrams/logout.svg) · [PlantUML source](diagrams/logout.puml)

<a id="8-other-browser-tabs"></a>

## Browser tabs

Each tab has its own in-memory session. On the web, `session.subscribeToChanges()` listens for messages written to `localStorage` by other tabs. `SignedOut` clears the receiving tab's local state. `Changed`, sent after successful login, prompts it to restore the session from the shared cookie. A Web Lock serializes auth operations across tabs. [Web sync](../../../mobile/src/shared/session/session-sync.web.ts)

[Tab sync diagram](diagrams/tab-sync.svg) · [PlantUML source](diagrams/tab-sync.puml)

<a id="9-storage-and-store-methods"></a>

## Token storage

| Data | Native | Web |
|---|---|---|
| Access token | In memory | In memory in each tab |
| Refresh token | SecureStore | `HttpOnly` cookie |
| `LogoutPending` | SecureStore | `localStorage` |

Native auth endpoints are under `/api/auth/mobile/` and receive the refresh token in JSON for refresh and logout. Web endpoints are under `/api/auth/` and use the cookie. [Auth endpoints](../../../mobile/src/features/auth/api/auth-api.ts)

## Verification

Run `npm test`, `npm run typecheck`, and `npm run lint` from the mobile app directory. [Session tests](../../../mobile/tests/session.test.cjs)
