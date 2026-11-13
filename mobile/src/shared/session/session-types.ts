// This file contains the definition of session-related types.
// It includes the authentication status and session tokens.

export type AuthStatus =
  | "anonymous"
  | "authenticated"
  | "restoring"
  | "restore-error";

export type SessionChangeType = "signed-in" | "signed-out";

export type SessionChange = { id: string; type: SessionChangeType };

export type SessionSync = {
  read(): SessionChange | null;
  publish(type: SessionChangeType): SessionChange | null;
  subscribe(listener: () => void): () => void;
};

export type SessionTokens = {
  accessToken: string;
  expiresAt: string;
};
