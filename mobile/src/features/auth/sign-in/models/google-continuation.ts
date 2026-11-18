export type GoogleContinuation = {
  status: "linkRequired" | "registrationRequired";
  continuationToken: string;
  continuationExpiresAtUtc: string;
};

export function isGoogleContinuationExpired(continuation: GoogleContinuation): boolean {
  const expiresAt = Date.parse(continuation.continuationExpiresAtUtc);
  return !Number.isFinite(expiresAt) || expiresAt <= Date.now();
}
