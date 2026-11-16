import { useCallback, useEffect, useRef, useState } from "react";

import { useApi } from "@/shared/api/api-context";
import { useSessionManager } from "@/shared/session/AuthProvider";
import { createGoogleLinkChallenge, linkGoogleAccount } from "../api/google-account-link";
import { getGoogleLinkError } from "../models/google-link-error";

type Step =
  | { kind: "retry"; error?: string }
  | { kind: "preparing" }
  | { kind: "google"; nonce: string }
  | { kind: "linking" }
  | { kind: "linked" };

type Attempt = {
  controller: AbortController;
  generation: number;
  nonce?: string;
  sending: boolean;
};

export function useGoogleAccountLink() {
  const api = useApi();
  const manager = useSessionManager();
  const current = useRef<Attempt | null>(null);
  const [step, setStep] = useState<Step>({ kind: "preparing" });

  const settle = useCallback((attempt: Attempt, nextStep: Step) => {
    if (current.current !== attempt) return;
    current.current = null;
    attempt.controller.abort();
    setStep(nextStep);
  }, []);

  const begin = useCallback(() => {
    if (current.current) return;

    const attempt: Attempt = {
      controller: new AbortController(),
      generation: manager.getGeneration(),
      sending: false,
    };
    current.current = attempt;

    void createGoogleLinkChallenge(api, attempt.controller.signal)
      .then((nonce) => {
        if (current.current !== attempt) return;
        manager.assertCurrent(attempt.generation);
        attempt.nonce = nonce;
        setStep({ kind: "google", nonce });
      })
      .catch((error: unknown) => {
        settle(attempt, { kind: "retry", error: getGoogleLinkError(error) });
      });
  }, [api, manager, settle]);

  useEffect(() => {
    begin();
    return () => {
      const attempt = current.current;
      current.current = null;
      attempt?.controller.abort();
    };
  }, [begin]);

  const complete = useCallback(async (nonce: string, idToken: string) => {
    const attempt = current.current;
    if (!attempt || attempt.nonce !== nonce || attempt.sending) return;

    attempt.sending = true;
    setStep({ kind: "linking" });
    try {
      manager.assertCurrent(attempt.generation);
      await linkGoogleAccount(api, idToken, attempt.controller.signal);
      manager.assertCurrent(attempt.generation);
      settle(attempt, { kind: "linked" });
    } catch (error) {
      settle(attempt, { kind: "retry", error: getGoogleLinkError(error) });
    }
  }, [api, manager, settle]);

  const cancel = useCallback((nonce: string) => {
    const attempt = current.current;
    if (attempt?.nonce === nonce && !attempt.sending) {
      settle(attempt, { kind: "retry" });
    }
  }, [settle]);

  const failGoogle = useCallback((nonce: string) => {
    const attempt = current.current;
    if (attempt?.nonce === nonce && !attempt.sending) {
      settle(attempt, { kind: "retry", error: "Could not open Google. Please try again." });
    }
  }, [settle]);

  function retry() {
    if (current.current) return;
    setStep({ kind: "preparing" });
    begin();
  }

  return { step, retry, complete, cancel, failGoogle };
}
