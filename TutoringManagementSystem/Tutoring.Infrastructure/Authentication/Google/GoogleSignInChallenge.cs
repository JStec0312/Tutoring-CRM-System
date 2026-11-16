namespace Tutoring.Infrastructure.Authentication.Google;

public sealed record GoogleSignInChallenge(
    string Nonce,
    Guid? AccountId,
    DateTimeOffset ExpiresAtUtc);
