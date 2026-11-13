export interface RefreshTokenStorage {
  readonly native: boolean;

  readRefreshToken(): Promise<string | null>;

  writeRefreshToken(token: string): Promise<void>;

  deleteRefreshToken(): Promise<void>;

  isLogoutPending(): Promise<boolean>;

  setLogoutPending(pending: boolean): Promise<void>;
}
