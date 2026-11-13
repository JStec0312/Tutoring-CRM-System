import * as SecureStore from "expo-secure-store";

import type { RefreshTokenStorage } from "./refresh-token-storage.types";

const STORAGE_KEYS = {
  refreshToken: "tutoring.session.refresh-token",
  logoutPending: "tutoring.session.logout-pending",
} as const;

const SECURE_OPTIONS: SecureStore.SecureStoreOptions = {
  keychainAccessible: SecureStore.WHEN_UNLOCKED_THIS_DEVICE_ONLY,
};

export const refreshTokenStorage: RefreshTokenStorage = {
  native: true,

  readRefreshToken() {
    return SecureStore.getItemAsync(STORAGE_KEYS.refreshToken, SECURE_OPTIONS);
  },

  async writeRefreshToken(token) {
    if (!token.trim()) {
      throw new Error("Session.EmptyRefreshToken");
    }

    await SecureStore.setItemAsync(
      STORAGE_KEYS.refreshToken,
      token,
      SECURE_OPTIONS,
    );
  },

  async deleteRefreshToken() {
    await SecureStore.deleteItemAsync(
      STORAGE_KEYS.refreshToken,
      SECURE_OPTIONS,
    );
  },

  async isLogoutPending() {
    const value = await SecureStore.getItemAsync(
      STORAGE_KEYS.logoutPending,
      SECURE_OPTIONS,
    );

    return value === "true";
  },

  async setLogoutPending(pending) {
    if (pending) {
      await SecureStore.setItemAsync(
        STORAGE_KEYS.logoutPending,
        "true",
        SECURE_OPTIONS,
      );
    } else {
      await SecureStore.deleteItemAsync(
        STORAGE_KEYS.logoutPending,
        SECURE_OPTIONS,
      );
    }
  },
};
