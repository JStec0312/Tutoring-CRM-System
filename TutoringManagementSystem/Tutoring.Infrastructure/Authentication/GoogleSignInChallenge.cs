namespace Tutoring.Infrastructure.Authentication;

public sealed record GoogleSignInChallenge(
    string Nonce,
    Guid? AccountId,
    DateTimeOffset ExpiresAtUtc);
