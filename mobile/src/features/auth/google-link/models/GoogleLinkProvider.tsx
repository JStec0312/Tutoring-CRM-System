import { createContext, useContext, useEffect, useState, type PropsWithChildren } from "react";

import type { GoogleLinkContinuation } from "./google-link-continuation";

type GoogleLinkState = {
  continuation: GoogleLinkContinuation | null;
  setContinuation: (continuation: GoogleLinkContinuation | null) => void;
};

const GoogleLinkContext = createContext<GoogleLinkState | null>(null);

export function GoogleLinkProvider({ children }: PropsWithChildren) {
  const [continuation, setContinuation] = useState<GoogleLinkContinuation | null>(null);

  useEffect(() => {
    if (!continuation) return;

    const remaining = Date.parse(continuation.continuationExpiresAtUtc) - Date.now();
    const timer = setTimeout(() => setContinuation(null), Math.max(0, remaining));
    return () => clearTimeout(timer);
  }, [continuation]);

  return (
    <GoogleLinkContext value={{ continuation, setContinuation }}>
      {children}
    </GoogleLinkContext>
  );
}

export function useGoogleLink() {
  const state = useContext(GoogleLinkContext);

  if (!state) {
    throw new Error("GoogleLinkProvider is missing");
  }

  return state;
}
