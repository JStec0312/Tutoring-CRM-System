import type { SessionStore } from './session-store';

export const SessionStatus = {
  Restoring: 'restoring',
  Authenticated: 'authenticated',
  Anonymous: 'anonymous',
  RestoreError: 'restore-error',
} as const;

export const Role = { Tutor: 'Tutor', Student: 'Student', Administrator: 'Administrator' } as const;
export type UserRole = (typeof Role)[keyof typeof Role];
export type User = Readonly<{ id: string; email: string; roles: readonly UserRole[] }>;
export type AccessToken = Readonly<{ value: string; expiresAt: number }>;
export type SessionSnapshot = Readonly<{
  status: (typeof SessionStatus)[keyof typeof SessionStatus];
  user: User | null;
  error: string | null;
}>;

export type Credentials = { email: string; password: string };
export type Session = {
  readonly store: SessionStore;
  signIn(credentials: Credentials): Promise<void>;
  signOut(): Promise<void>;
  restoreSession(): Promise<void>;
  ensureAccessToken(): Promise<string>;
  refresh(): Promise<void>;
  invalidate(): Promise<void>;
  subscribeToChanges(): () => void;
};

export const SessionError = {
  Required: 'Sign in to continue.',
  Obsolete: 'The session changed. Discarding the previous operation.',
  InvalidResponse: 'The API returned invalid session data.',
  LogoutPending: 'Finish signing out or sign in again to continue.',
  Storage: 'Could not securely save the session. Please sign in again.',
  Revocation: 'Signed out locally. Server revocation could not be confirmed. Sign in again or retry signing out.',
  WebLock: 'Session coordination requires a browser with Web Locks support in a secure context (HTTPS or localhost).',
} as const;

export class SessionChangedError extends Error {
  constructor() { super(SessionError.Obsolete); }
}

export class InvalidSessionResponseError extends Error {
  constructor() { super(SessionError.InvalidResponse); }
}
