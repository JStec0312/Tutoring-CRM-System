import type { RefreshTokenStorage } from "./refresh-token-storage.types";

const LOGOUT_PENDING_KEY = "tutoring.session.logout-pending";

function getBrowserStorage(): Storage {
  if (typeof window === "undefined") {
    throw new Error("Session.BrowserStorageUnavailable");
  }

  return window.localStorage;
}

export const refreshTokenStorage: RefreshTokenStorage = {
  native: false,

  async readRefreshToken() {
    // HttpOnly cookie is not accessible from JavaScript.
    return null;
  },

  async writeRefreshToken(_token) {
    throw new Error("Web refresh token must stay in an HttpOnly cookie");
  },

  async deleteRefreshToken() {
    // The backend deletes the HttpOnly cookie on sign-out.
  },

  async isLogoutPending() {
    return getBrowserStorage().getItem(LOGOUT_PENDING_KEY) === "true";
  },

  async setLogoutPending(pending) {
    const storage = getBrowserStorage();

    if (pending) {
      storage.setItem(LOGOUT_PENDING_KEY, "true");
    } else {
      storage.removeItem(LOGOUT_PENDING_KEY);
    }
  },
};
