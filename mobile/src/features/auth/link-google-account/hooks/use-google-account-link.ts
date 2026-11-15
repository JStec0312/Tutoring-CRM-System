import { useCallback, useEffect, useRef, useState } from "react";

import { useApi } from "@/shared/api/api-context";
import { useSessionManager } from "@/shared/session/AuthProvider";
import { createGoogleLinkChallenge, linkGoogleAccount } from "../api/google-account-link";
import { getGoogleLinkError } from "../models/google-link-error";

type Step =
  | { kind: "password"; error?: string }
  | { kind: "preparing" }
  | { kind: "google"; nonce: string }
  | { kind: "linking" }
  | { kind: "linked" };

type Attempt = {
  controller: AbortController;
  generation: number;
  password: string;
  nonce?: string;
  sending: boolean;
};

export function useGoogleAccountLink() {
  const api = useApi();
  const manager = useSessionManager();
  const current = useRef<Attempt | null>(null);
  const [step, setStep] = useState<Step>({ kind: "password" });

  useEffect(() => () => {
    current.current?.controller.abort();
    current.current = null;
  }, []);

  const settle = useCallback((attempt: Attempt, nextStep: Step) => {
    if (current.current !== attempt) return;
    current.current = null;
    attempt.controller.abort();
    setStep(nextStep);
  }, []);

  async function begin(password: string) {
    if (current.current) return;
    if (!password.trim()) {
      setStep({ kind: "password", error: "Enter your current CRM password." });
      return;
    }

    const attempt: Attempt = {
      controller: new AbortController(),
      generation: manager.getGeneration(),
      password,
      sending: false,
    };
    current.current = attempt;
    setStep({ kind: "preparing" });

    try {
      const nonce = await createGoogleLinkChallenge(api, attempt.controller.signal);
      if (current.current !== attempt) return;
      manager.assertCurrent(attempt.generation);
      attempt.nonce = nonce;
      setStep({ kind: "google", nonce });
    } catch (error) {
      settle(attempt, { kind: "password", error: getGoogleLinkError(error) });
    }
  }

  const complete = useCallback(async (nonce: string, idToken: string) => {
    const attempt = current.current;
    if (!attempt || attempt.nonce !== nonce || attempt.sending) return;

    attempt.sending = true;
    setStep({ kind: "linking" });
    try {
      manager.assertCurrent(attempt.generation);
      await linkGoogleAccount(api, idToken, attempt.password, attempt.controller.signal);
      manager.assertCurrent(attempt.generation);
      settle(attempt, { kind: "linked" });
    } catch (error) {
      settle(attempt, { kind: "password", error: getGoogleLinkError(error) });
    }
  }, [api, manager, settle]);

  const cancel = useCallback((nonce: string) => {
    const attempt = current.current;
    if (attempt?.nonce === nonce && !attempt.sending) {
      settle(attempt, { kind: "password" });
    }
  }, [settle]);

  const failGoogle = useCallback((nonce: string) => {
    const attempt = current.current;
    if (attempt?.nonce === nonce && !attempt.sending) {
      settle(attempt, { kind: "password", error: "Could not open Google. Please try again." });
    }
  }, [settle]);

  return { step, begin, complete, cancel, failGoogle };
}
