import { createContext, useContext, useEffect, useState, type PropsWithChildren } from "react";

import type { GoogleContinuation } from "./google-continuation";

type GoogleContinuationState = {
  continuation: GoogleContinuation | null;
  setContinuation: (continuation: GoogleContinuation | null) => void;
};

const GoogleContinuationContext = createContext<GoogleContinuationState | null>(null);

export function GoogleContinuationProvider({ children }: PropsWithChildren) {
  const [continuation, setContinuation] = useState<GoogleContinuation | null>(null);

  useEffect(() => {
    if (!continuation) return;

    const remaining = Date.parse(continuation.continuationExpiresAtUtc) - Date.now();
    const timer = setTimeout(() => setContinuation(null), Math.max(0, remaining));
    return () => clearTimeout(timer);
  }, [continuation]);

  return (
    <GoogleContinuationContext value={{ continuation, setContinuation }}>
      {children}
    </GoogleContinuationContext>
  );
}

export function useGoogleContinuation() {
  const state = useContext(GoogleContinuationContext);

  if (!state) {
    throw new Error("GoogleContinuationProvider is missing");
  }

  return state;
}
