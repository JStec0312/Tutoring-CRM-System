import { useEffect, useRef, useState } from "react";
import { ActivityIndicator } from "react-native";

import { googleWebClientId } from "../models/google-link-platform";
import type { GoogleCredentialButtonProps } from "./google-credential-button-props";

type GoogleModule = typeof import("react-native-nitro-google-signin");

export function GoogleCredentialButton({ nonce, onCredential, onFailure, onCancel }: GoogleCredentialButtonProps) {
  const [google, setGoogle] = useState<GoogleModule | null>(null);
  const [pending, setPending] = useState(false);
  const active = useRef(false);
  const opening = useRef(false);

  useEffect(() => {
    active.current = true;
    void import("react-native-nitro-google-signin").then((module) => {
      if (active.current) setGoogle(module);
    }, () => {
      if (active.current) onFailure(nonce);
    });

    return () => {
      active.current = false;
    };
  }, [nonce, onFailure]);

  async function openGoogle() {
    if (!google || opening.current) return;

    opening.current = true;
    setPending(true);
    try {
      const { GoogleOneTapSignIn, isCancelledResponse, isSuccessResponse } = google;
      GoogleOneTapSignIn.configure({ webClientId: googleWebClientId, nonce });
      await GoogleOneTapSignIn.checkPlayServices();
      if (!active.current) return;

      const response = await GoogleOneTapSignIn.presentExplicitSignIn();
      if (!active.current) return;

      if (isCancelledResponse(response)) {
        onCancel(nonce);
      } else if (isSuccessResponse(response) && response.data.idToken) {
        onCredential(nonce, response.data.idToken);
      } else {
        onFailure(nonce);
      }
    } catch {
      if (active.current) onFailure(nonce);
    } finally {
      opening.current = false;
      if (active.current) setPending(false);
    }
  }

  if (!google) return <ActivityIndicator accessibilityLabel="Loading Google" />;

  const { GoogleSignInButton } = google;
  return (
    <GoogleSignInButton
      accessibilityLabel="Continue with Google"
      accessibilityRole="button"
      colorScheme="light"
      size="standard"
      signInBehavior="none"
      disabled={pending}
      loading={pending}
      onPress={openGoogle}
    />
  );
}
