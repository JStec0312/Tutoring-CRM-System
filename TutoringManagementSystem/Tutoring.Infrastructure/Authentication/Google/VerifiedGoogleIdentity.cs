namespace Tutoring.Infrastructure.Authentication.Google;

public sealed record VerifiedGoogleIdentity(string Subject, string Nonce);
