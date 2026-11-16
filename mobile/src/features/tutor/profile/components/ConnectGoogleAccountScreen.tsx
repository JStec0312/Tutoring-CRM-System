import { Link } from "expo-router";

import { FormScreen, formStyles } from "@/shared/ui/FormScreen";
import { ConnectGoogleAccountContent } from "./ConnectGoogleAccountButton";

export function ConnectGoogleAccountScreen() {
  return (
    <FormScreen title="Connect Google account">
      <ConnectGoogleAccountContent />
      <Link href="/" style={formStyles.link}>Back to portal</Link>
    </FormScreen>
  );
}
