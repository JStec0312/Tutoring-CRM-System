export const SessionStorageKey = {
  RefreshToken: 'tutoring.session.refresh-token',
  LogoutPending: 'tutoring.session.logout-pending',
} as const;
export const StorageFlag = { True: 'true' } as const;

export interface CredentialStorage {
  readonly native: boolean;
  readRefreshToken(): Promise<string | null>;
  writeRefreshToken(value: string): Promise<void>;
  deleteRefreshToken(): Promise<void>;
  isLogoutPending(): Promise<boolean>;
  setLogoutPending(pending: boolean): Promise<void>;
  exclusive<T>(operation: () => Promise<T>): Promise<T>;
}
