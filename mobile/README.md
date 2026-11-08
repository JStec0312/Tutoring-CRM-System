# Tutoring CRM mobile

Expo SDK 58 / React Native. Feature-based modules; shared HTTP and session infrastructure.

## Run

1. `npm install` (supported Node release: 22.13+ or 24.3+ LTS).
2. Copy `.env.example` to `.env.local`; set `EXPO_PUBLIC_API_URL` to the backend origin reachable from your device. `localhost` on a physical phone refers to the phone itself.
3. `npm start` (or `npm run web`).

Native authentication uses `/api/auth/mobile/*` and SecureStore. Web uses `/api/auth/*`, HttpOnly cookies and Web Locks; serve over HTTPS or localhost. Configure credentialed CORS/cookie/CSRF policy for your deployment. No passwords/access tokens in persistent storage.

## Develop

- `src/app`: route composition and protection.
- `src/bootstrap`: creates auth/controller/client once and injects dependencies.
- `src/features`: business UI, models, API mappers and hooks. Reserved feature folders contain a short README until implemented.
- `src/shared`: UI, theme/hooks, HTTP client, session state and storage adapters.
- `useApi()` exposes one authenticated client; `useSession()` exposes public state and actions. No per-screen token or refresh logic.
- `src/features/explore` retains the starter examples; business screens can replace them incrementally.

Checks: `npm run typecheck`, `npm run lint`, `npm test`.

[Architecture and session implementation](../docs/mobile/architecture/session-implementation.md).
