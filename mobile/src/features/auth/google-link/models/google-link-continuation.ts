export type GoogleLinkContinuation = {
  continuationToken: string;
  continuationExpiresAtUtc: string;
};

export function isGoogleLinkExpired(continuation: GoogleLinkContinuation): boolean {
  const expiresAt = Date.parse(continuation.continuationExpiresAtUtc);
  return !Number.isFinite(expiresAt) || expiresAt <= Date.now();
}
