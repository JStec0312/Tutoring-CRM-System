import { ActivityIndicator, Pressable, Text } from "react-native";

import { FormSubmit, formStyles } from "@/shared/ui/FormScreen";
import { useGoogleAccountLink } from "../hooks/use-google-account-link";
import { getGoogleLinkUnavailableReason, supportsGoogleAccountLink } from "../models/google-link-platform";
import { GoogleCredentialButton } from "./GoogleCredentialButton";

export function ConnectGoogleAccountButton() {
  if (!supportsGoogleAccountLink) return null;

  return (
    <>
      <Text accessibilityRole="header" style={formStyles.description}>Połącz konto Google</Text>
      <ConnectGoogleAccountContent locale="pl" showInstructions={false} />
    </>
  );
}

export function ConnectGoogleAccountContent({ locale, showInstructions = true }: {
  locale?: string;
  showInstructions?: boolean;
}) {
  const unavailable = getGoogleLinkUnavailableReason();
  if (unavailable) return <Text style={formStyles.description}>{unavailable}</Text>;

  return <ConnectGoogleAccountFlow locale={locale} showInstructions={showInstructions} />;
}

function ConnectGoogleAccountFlow({ locale, showInstructions }: {
  locale?: string;
  showInstructions: boolean;
}) {
  const { step, retry, complete, cancel, failGoogle } = useGoogleAccountLink();

  if (step.kind === "linked") {
    return (
      <Text accessibilityLiveRegion="polite" style={formStyles.description}>
        Your Google account is now connected to your CRM account.
      </Text>
    );
  }

  if (step.kind === "linking") {
    return <ActivityIndicator accessibilityLabel="Connecting Google account" />;
  }

  if (step.kind === "google") {
    return (
      <>
        {showInstructions && <Text style={formStyles.description}>Choose the Google account you want to connect.</Text>}
        <GoogleCredentialButton
          key={step.nonce}
          nonce={step.nonce}
          locale={locale}
          onCredential={complete}
          onFailure={failGoogle}
          onCancel={cancel}
        />
        {showInstructions && (
          <Pressable accessibilityRole="button" onPress={() => cancel(step.nonce)}>
            <Text style={formStyles.link}>Cancel</Text>
          </Pressable>
        )}
      </>
    );
  }

  if (step.kind === "preparing") {
    return <ActivityIndicator accessibilityLabel="Preparing Google account linking" />;
  }

  return (
    <>
      {step.error && <Text accessibilityRole="alert" style={formStyles.error}>{step.error}</Text>}
      <FormSubmit label="Try again" pending={false} onPress={retry} />
    </>
  );
}
