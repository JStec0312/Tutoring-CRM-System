import { afterEach, expect, it, vi } from "vitest";

import { sessionSync } from "@/shared/session/session-sync.web";

afterEach(() => vi.unstubAllGlobals());

it("publishes only event metadata and listens to the shared local storage key", () => {
  const values = new Map<string, string>();
  const localStorage = {
    getItem: (key: string) => values.get(key) ?? null,
    setItem: (key: string, value: string) => values.set(key, value),
  };
  const browser = Object.assign(new EventTarget(), {
    localStorage,
    crypto: { randomUUID: () => "event-id" },
  });
  vi.stubGlobal("window", browser);
  const listener = vi.fn();
  const unsubscribe = sessionSync.subscribe(listener);

  expect(sessionSync.publish("signed-in")).toEqual({ id: "event-id", type: "signed-in" });
  const [key] = values.keys();
  expect(JSON.parse(values.get(key)!)).toEqual({ id: "event-id", type: "signed-in" });
  expect(listener).not.toHaveBeenCalled();

  function deliver(storageKey: string) {
    browser.dispatchEvent(Object.assign(new Event("storage"), {
      key: storageKey,
      storageArea: localStorage,
    }));
  }

  deliver("other-key");
  expect(listener).not.toHaveBeenCalled();
  deliver(key);
  expect(listener).toHaveBeenCalledOnce();
  expect(sessionSync.read()).toEqual({ id: "event-id", type: "signed-in" });
  unsubscribe();
  deliver(key);
  expect(listener).toHaveBeenCalledOnce();
});
