// This file contains the definition of session-related types.
// It includes the authentication status and session tokens.

export type AuthStatus =
  | "anonymous"
  | "authenticated"
  | "restoring"
  | "restore-error";

export type SessionTokens = {
  accessToken: string;
  expiresAt: string;
};
