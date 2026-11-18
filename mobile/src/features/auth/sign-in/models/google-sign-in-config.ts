export function getGoogleWebClientId(): string {
  const clientId = process.env.EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID;

  if (!clientId) {
    throw new Error("Google.ClientIdMissing");
  }

  return clientId;
}
