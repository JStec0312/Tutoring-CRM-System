import { useRef, useState } from "react";
import { Platform } from "react-native";

import { useApi } from "@/shared/api/api-context";
import { useAuth } from "@/shared/session/AuthProvider";
import type { RegistrationRole } from "@/shared/session/registration-role";
import { FormError } from "@/shared/ui/FormError";
import { FormField, FormSubmit } from "@/shared/ui/FormScreen";
import { FormTextButton } from "@/shared/ui/FormTextButton";
import { RegistrationRolePicker } from "@/shared/ui/RegistrationRolePicker";
import type { GoogleContinuation } from "../../sign-in/models/google-continuation";
import { registerWithGoogle } from "../api/register-with-google";
import { getGoogleRegistrationError } from "../models/google-registration-error";

type Props = {
  continuation: GoogleContinuation;
  pending: boolean;
  onPendingChange: (pending: boolean) => void;
  onRegistered: () => void;
  onCancel: () => void;
};

export function GoogleRegistrationForm({ continuation, pending, onPendingChange, onRegistered, onCancel }: Props) {
  const api = useApi();
  const { signInWith } = useAuth();
  const [role, setRole] = useState<RegistrationRole | null>(null);
  const [userName, setUserName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const submitting = useRef(false);

  async function handleRegister() {
    if (submitting.current) return;

    if (!role) {
      setError("Choose an account type.");
      return;
    }
    if (!userName.trim()) {
      setError("Enter a username.");
      return;
    }

    submitting.current = true;
    onPendingChange(true);
    setError(null);
    try {
      await signInWith(() => registerWithGoogle(api, continuation, role, userName, Platform.OS !== "web"));
      onRegistered();
    } catch (error) {
      setError(getGoogleRegistrationError(error));
    } finally {
      submitting.current = false;
      onPendingChange(false);
    }
  }

  return (
    <>
      <RegistrationRolePicker role={role} onChange={setRole} disabled={pending} />
      <FormField
        label="Username"
        value={userName}
        onChangeText={setUserName}
        autoCapitalize="none"
        autoCorrect={false}
        autoComplete="username"
        maxLength={100}
        returnKeyType="go"
        onSubmitEditing={handleRegister}
        editable={!pending}
      />
      <FormError message={error} />
      <FormSubmit label="Create account" pending={pending} onPress={handleRegister} />
      <FormTextButton label="Cancel" disabled={pending} onPress={onCancel} />
    </>
  );
}
