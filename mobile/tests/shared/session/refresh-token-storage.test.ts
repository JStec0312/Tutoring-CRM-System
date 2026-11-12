import { afterEach, describe, expect, it, vi } from "vitest";

import { refreshTokenStorage } from "../../../src/shared/session/refresh-token-storage.web";

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("Web RefreshTokenStorage", () => {
  function setupStorage() {
    const values = new Map<string, string>();

    vi.stubGlobal("window", {
      localStorage: {
        getItem: (key: string) => values.get(key) ?? null,
        setItem: (key: string, value: string) => {
          values.set(key, value);
        },
        removeItem: (key: string) => {
          values.delete(key);
        },
      },
    });
  }

  it("never exposes refresh token in JavaScript", async () => {
    expect(await refreshTokenStorage.readRefreshToken()).toBeNull();

    await expect(
      refreshTokenStorage.writeRefreshToken("secret"),
    ).rejects.toThrow();
  });

  it("persists pending logout marker", async () => {
    setupStorage();

    expect(await refreshTokenStorage.isLogoutPending()).toBe(false);

    await refreshTokenStorage.setLogoutPending(true);

    expect(await refreshTokenStorage.isLogoutPending()).toBe(true);

    await refreshTokenStorage.setLogoutPending(false);

    expect(await refreshTokenStorage.isLogoutPending()).toBe(false);
  });

  it("fails if browser storage is unavailable", async () => {
    await expect(refreshTokenStorage.isLogoutPending()).rejects.toThrow(
      "Session.BrowserStorageUnavailable",
    );
  });
});
