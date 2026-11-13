import type { SessionChange, SessionSync } from "./session-types";

const KEY = "tutoring.session.change";

export const sessionSync: SessionSync = {
  read() {
    if (typeof window === "undefined") return null;

    try {
      const value = window.localStorage.getItem(KEY);
      if (!value) return null;

      const change: SessionChange | null = JSON.parse(value);
      if (
        typeof change?.id === "string" &&
        (change.type === "signed-in" || change.type === "signed-out")
      ) {
        return change;
      }
    } catch {
      // Ignore malformed values.
    }

    return null;
  },

  publish(type) {
    if (typeof window === "undefined") return null;

    try {
      const change = { id: window.crypto.randomUUID(), type };
      window.localStorage.setItem(KEY, JSON.stringify(change));
      return change;
    } catch {
      // Browser storage can be disabled or full.
      return null;
    }
  },

  subscribe(listener) {
    if (typeof window === "undefined") return () => undefined;

    function onStorage(event: StorageEvent) {
      if (event.key === KEY && event.storageArea === window.localStorage) {
        listener();
      }
    }

    window.addEventListener("storage", onStorage);
    return () => window.removeEventListener("storage", onStorage);
  },
};
