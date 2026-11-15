import { Link } from "expo-router";
import { useState } from "react";
import { ActivityIndicator, Pressable, Text } from "react-native";

import { FormField, FormScreen, FormSubmit, formStyles } from "@/shared/ui/FormScreen";
import { useGoogleAccountLink } from "../hooks/use-google-account-link";
import { getGoogleLinkUnavailableReason } from "../models/google-link-platform";
import { GoogleCredentialButton } from "./GoogleCredentialButton";

export function ConnectGoogleAccountScreen() {
  const unavailable = getGoogleLinkUnavailableReason();

  return (
    <FormScreen title="Connect Google account">
      {unavailable
        ? <Text style={formStyles.description}>{unavailable}</Text>
        : <ConnectGoogleAccountForm />}
      <Link href="/" style={formStyles.link}>Back to portal</Link>
    </FormScreen>
  );
}

function ConnectGoogleAccountForm() {
  const [password, setPassword] = useState("");
  const { step, begin, complete, cancel, failGoogle } = useGoogleAccountLink();

  function prepare() {
    void begin(password);
    if (password.trim()) setPassword("");
  }

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
        <Text style={formStyles.description}>Choose the Google account you want to connect.</Text>
        <GoogleCredentialButton
          key={step.nonce}
          nonce={step.nonce}
          onCredential={complete}
          onFailure={failGoogle}
          onCancel={cancel}
        />
        <Pressable accessibilityRole="button" onPress={() => cancel(step.nonce)}>
          <Text style={formStyles.link}>Cancel</Text>
        </Pressable>
      </>
    );
  }

  const preparing = step.kind === "preparing";
  return (
    <>
      <FormField
        label="Current CRM password"
        value={password}
        onChangeText={setPassword}
        maxLength={256}
        editable={!preparing}
        secureTextEntry
        autoComplete="current-password"
        returnKeyType="go"
        onSubmitEditing={prepare}
      />
      {step.kind === "password" && step.error && (
        <Text accessibilityRole="alert" style={formStyles.error}>{step.error}</Text>
      )}
      <FormSubmit label="Continue" pending={preparing} onPress={prepare} />
    </>
  );
}
