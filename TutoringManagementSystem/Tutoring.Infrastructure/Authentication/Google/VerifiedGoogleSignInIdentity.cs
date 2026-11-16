namespace Tutoring.Infrastructure.Authentication.Google;

public sealed record VerifiedGoogleSignInIdentity(
    string Subject, string Nonce, string? VerifiedEmail, bool IsEmailAuthoritative);
