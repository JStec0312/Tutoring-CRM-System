import { Link } from "expo-router";

import { formStyles } from "@/shared/ui/FormScreen";
import { supportsGoogleAccountLink } from "../models/google-link-platform";

export function ConnectGoogleAccountLink() {
  if (!supportsGoogleAccountLink) return null;

  return (
    <Link href="/connect-google" style={formStyles.link}>
      Connect Google account
    </Link>
  );
}
