import Constants, { ExecutionEnvironment } from "expo-constants";
import { Platform } from "react-native";

export const googleWebClientId = process.env.EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID?.trim() ?? "";
export const supportsGoogleAccountLink = Platform.OS === "web" || Platform.OS === "android";

export function getGoogleLinkUnavailableReason() {
  if (!supportsGoogleAccountLink) {
    return "Google account linking is available on web and Android.";
  }

  if (Platform.OS === "android" && Constants.executionEnvironment === ExecutionEnvironment.StoreClient) {
    return "Open a Tutoring CRM development build to connect your Google account on Android.";
  }

  if (!googleWebClientId) {
    return "Google account linking is not configured for this build.";
  }

  return null;
}
