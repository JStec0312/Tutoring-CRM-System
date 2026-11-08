import {
  CredentialStorage,
  SessionStorageKey,
  StorageFlag,
} from "./credential-storage.types";
import { SessionError } from "./session-types";

const AuthLockName = "tutoring.session.auth";

export const credentialStorage: CredentialStorage = {
  native: false,
  readRefreshToken: async () => null, // HttpOnly cookie is never read by JavaScript
  writeRefreshToken: async () => {
    throw new Error("Web refresh tokens must stay in HttpOnly cookies.");
  },
  deleteRefreshToken: async () => {},
  // imagine you want to log out and you lose internet connection, this flag helps track pending logout operations
  // this helps ensure that logout operations are not lost if the user goes offline
  isLogoutPending: async () =>
    localStorage.getItem(SessionStorageKey.LogoutPending) === StorageFlag.True,
  async setLogoutPending(pending) {
    if (pending)
      localStorage.setItem(SessionStorageKey.LogoutPending, StorageFlag.True);
    else localStorage.removeItem(SessionStorageKey.LogoutPending);
  },
  exclusive: (operation) =>
    navigator.locks
      ? navigator.locks.request(AuthLockName, operation)
      : Promise.reject(new Error(SessionError.WebLock)),
};
