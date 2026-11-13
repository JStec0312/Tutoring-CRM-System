# Application flow in code

**Design example, not production implementation.** Read sections in order; identifiers continue between blocks. Example: web client using the existing refresh-cookie contract. UI rendering is a port, independent of styling/framework. Native replaces auth transport with a secure-storage adapter after the backend contract is added.

## 1. Contracts and state — `shared/session`

```ts
type Token = { accessToken: string; expiresAt: string };
type SessionStatus = "restoring" | "anonymous" | "authenticated" | "restore-error";

let status: SessionStatus = "restoring";
let token: Token | undefined; // private; never passed to a screen
let generation = 0;          // changes on logout / account switch
let onSessionEnded = () => {}; // app supplies navigation callback

function assertCurrent(expected: number) {
  if (expected !== generation) throw new Error("Obsolete session operation");
}

function commitToken(next: Token, expected: number) {
  assertCurrent(expected);
  token = next;
  status = "authenticated";
}

function clearSession() {
  generation++;
  token = undefined;
  status = "anonymous";
  clearPrivateData(); // defined in section 4
  onSessionEnded();
}

// Public UI state: status and identity/roles, when added; never raw tokens.
const isLoggedIn = () => status === "authenticated";
```

## 2. Raw HTTP and authentication — `shared/api` + `features/auth`

`raw` never refreshes. Login/refresh/logout must use it to avoid recursion.

```ts
const API_URL = "https://api.example.test"; // deployment configuration

class HttpError extends Error {
  constructor(readonly status: number) { super(`HTTP ${status}`); }
}

async function raw(path: string, init: RequestInit = {}): Promise<Response> {
  const response = await fetch(API_URL + path, {
    ...init,
    credentials: "include", // browser sends/accepts HttpOnly refresh cookie
  });
  if (!response.ok) throw new HttpError(response.status);
  return response;
}

const rejected = (error: unknown) =>
  error instanceof HttpError && error.status === 401;

// Non-secret marker. Prevents restoring a cookie after failed server logout.
const logoutPending = () => localStorage.getItem("logoutPending") === "true";
let refreshing: Promise<void> | undefined;

function refreshOnce(): Promise<void> {
  if (logoutPending()) return Promise.reject(new Error("Logout pending"));
  if (refreshing) return refreshing; // all callers await the same request

  const expected = generation;
  refreshing = (async () => {
    try {
      const response = await raw("/api/auth/refresh", { method: "POST" });
      const next: Token = await response.json();
      // Browser already applied the rotated refresh cookie.
      commitToken(next, expected);
    } catch (error) {
      if (expected === generation && rejected(error)) clearSession();
      throw error; // network/5xx does not delete the session
    }
  })().finally(() => { refreshing = undefined; });
  return refreshing;
}

async function bootstrap() {
  status = "restoring";
  const expected = generation;
  if (logoutPending()) { status = "anonymous"; return; }
  try {
    await refreshOnce();
  } catch (error) {
    if (expected !== generation) return;
    status = rejected(error) ? "anonymous" : "restore-error";
  }
}

async function signIn(email: string, password: string) {
  clearSession(); // invalidate results/cache from any previous account
  const expected = generation;
  const response = await raw("/api/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });
  const next: Token = await response.json();
  commitToken(next, expected);
  localStorage.removeItem("logoutPending");
  // Password stays in form scope; reset the form after success.
}
```

Bootstrap: valid cookie → refresh → token in memory → authenticated. Missing/rejected cookie → login. Network failure → retry screen.

## 3. Authenticated requests — `shared/api/client`

Only this client reads the private access token. Example supports JSON GETs; uploads/mutations need their own replay policy.

```ts
function createApiClient(refresh: () => Promise<void>) {
  return {
    async get<T>(path: string): Promise<T> {
      if (logoutPending() || !isLoggedIn()) throw new Error("Sign in required");
      const expected = generation;

      const send = async () => {
        assertCurrent(expected);
        if (!token) throw new Error("No access token");
        const response = await raw(path, {
          headers: { Authorization: `Bearer ${token.accessToken}` },
        });
        const data: T = await response.json();
        assertCurrent(expected); // ignore response after logout/account switch
        return data;
      };

      if (!token || Date.parse(token.expiresAt) <= Date.now() + 30_000) {
        await refresh(); // refresh before using an expired/near-expiry token
      }
      assertCurrent(expected);
      const sentToken = token?.accessToken;
      try {
        return await send();
      } catch (error) {
        assertCurrent(expected);
        if (!rejected(error)) throw error; // includes 403 and network failures

        // Another request may already have refreshed this token.
        if (token?.accessToken === sentToken) await refresh();
        assertCurrent(expected);
        try {
          return await send(); // exactly one replay; no recursive retry
        } catch (retryError) {
          if (expected === generation && rejected(retryError)) clearSession();
          throw retryError;
        }
      }
    },
  };
}

// app composition root: shared client receives callback, not a feature import.
const api = createApiClient(refreshOnce);
```

## 4. Feature model and screen behavior — `features/lessons`

`LessonDto` and `/api/lessons` are illustrative; use the actual generated contract/endpoint when implementing.

```ts
type LessonDto = { id: string; scheduledStart: string };
type Lesson = { id: string; startsAt: Date };
const toLesson = (dto: LessonDto): Lesson => ({
  id: dto.id,
  startsAt: new Date(dto.scheduledStart),
});

async function getLessons(): Promise<Lesson[]> {
  return (await api.get<LessonDto[]>("/api/lessons")).map(toLesson);
}

type LessonsView =
  | { state: "loading" }
  | { state: "ready"; items: Lesson[] } // [] renders the empty state
  | { state: "error"; message: string };

// UI port: implementation renders native/web components from this model.
declare function renderLessons(view: LessonsView): void;
declare function renderRoute(route: "splash" | "sign-in" | "retry" | "lessons"): void;

let privateLessons: Lesson[] = []; // minimal stand-in for a query cache
let loadVersion = 0;

function clearPrivateData() {
  privateLessons = [];
  loadVersion++; // invalidate in-flight screen loads
}

async function loadLessons() {
  const expected = generation;
  const load = ++loadVersion;
  const current = () => expected === generation && load === loadVersion;
  renderLessons({ state: "loading" });
  try {
    const items = await getLessons();
    if (!current()) return;
    privateLessons = items;
    renderLessons({ state: "ready", items: privateLessons });
  } catch (error) {
    if (!current()) return;
    renderLessons({
      state: "error",
      message: error instanceof Error ? error.message : "Request failed",
    });
  }
}
```

Change `scheduledStart` in the backend → update generated DTO and `toLesson`. UI still receives `Lesson.startsAt`. Styling changes affect only `renderLessons`/components.

## 5. Entry point, UI actions and logout — `app` + `features/auth`

```ts
async function showCurrentRoute() {
  if (status === "restoring") return renderRoute("splash");
  if (status === "restore-error") return renderRoute("retry");
  if (status === "anonymous") return renderRoute("sign-in");
  renderRoute("lessons");
  await loadLessons();
}

async function startApp() {
  renderRoute("splash");
  await bootstrap();
  await showCurrentRoute();
}

async function onLoginSubmit(email: string, password: string) {
  await signIn(email, password); // form displays errors and disables resubmission
  await showCurrentRoute();
}

async function onRestoreRetry() { await startApp(); }
async function onLessonsRetry() { await loadLessons(); }

async function onLogout() {
  localStorage.setItem("logoutPending", "true");
  clearSession(); // immediate: token, identity, cache; old results become obsolete
  renderRoute("sign-in");
  // Wait for old refresh to finish so sign-out revokes its rotated cookie too.
  await refreshing?.catch(() => undefined);
  await raw("/api/auth/sign-out", { method: "POST" });
  localStorage.removeItem("logoutPending");
  // Failure propagates to UI; marker remains, local session stays cleared.
}

// app: request/refresh rejection must also leave protected screens.
onSessionEnded = () => renderRoute("sign-in");

// Bind UI events once:
// Login form -> onLoginSubmit; retry buttons -> corresponding retry handlers.
// Logout button -> onLogout. Disable overlapping auth actions until completion.
// Application entry -> startApp().
```

Production additions: reactive session notifications, generated DTOs/runtime validation, structured API errors, real account-scoped query cache, cross-tab refresh/logout coordination, request cancellation and platform storage error handling. Never clear another session's credentials while discarding an obsolete result. See [architecture rules](README.md) for the platform boundary.
