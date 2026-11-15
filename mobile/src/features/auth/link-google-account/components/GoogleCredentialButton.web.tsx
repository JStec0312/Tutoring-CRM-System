import { useEffect, useRef, useState } from "react";
import { ActivityIndicator } from "react-native";

import { googleWebClientId } from "../models/google-link-platform";
import type { GoogleCredentialButtonProps } from "./google-credential-button-props";

type GoogleIdentity = {
  initialize(options: {
    client_id: string;
    nonce: string;
    ux_mode: "popup";
    callback: (response: { credential?: string }) => void;
  }): void;
  renderButton(element: HTMLElement, options: {
    theme: "outline";
    size: "large";
    text: "continue_with";
  }): void;
};

function getGoogleIdentity() {
  const browser = window as Window & { google?: { accounts?: { id?: GoogleIdentity } } };
  return browser.google?.accounts?.id;
}

let scriptLoad: Promise<GoogleIdentity> | undefined;

function loadGoogleIdentity() {
  const identity = getGoogleIdentity();
  if (identity) return Promise.resolve(identity);
  if (scriptLoad) return scriptLoad;

  const script = document.createElement("script");
  script.src = "https://accounts.google.com/gsi/client";
  script.async = true;
  scriptLoad = new Promise<GoogleIdentity>((resolve, reject) => {
    script.onload = () => {
      const loaded = getGoogleIdentity();
      if (loaded) resolve(loaded);
      else reject(new Error("Google Identity Services is unavailable"));
    };
    script.onerror = () => reject(new Error("Google Identity Services could not load"));
    document.head.appendChild(script);
  }).catch((error) => {
    script.remove();
    scriptLoad = undefined;
    throw error;
  });

  return scriptLoad;
}

export function GoogleCredentialButton({ nonce, onCredential, onFailure }: GoogleCredentialButtonProps) {
  const container = useRef<HTMLDivElement>(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const element = container.current;
    if (!element) return;

    let active = true;
    void loadGoogleIdentity().then((identity) => {
      if (!active) return;

      identity.initialize({
        client_id: googleWebClientId,
        nonce,
        ux_mode: "popup",
        callback: ({ credential }) => {
          if (!active) return;
          if (credential) onCredential(nonce, credential);
          else onFailure(nonce);
        },
      });
      identity.renderButton(element, { theme: "outline", size: "large", text: "continue_with" });
      setReady(true);
    }).catch(() => {
      if (active) onFailure(nonce);
    });

    return () => {
      active = false;
      element.replaceChildren();
    };
  }, [nonce, onCredential, onFailure]);

  return (
    <>
      {!ready && <ActivityIndicator accessibilityLabel="Loading Google" />}
      <div ref={container} />
    </>
  );
}
