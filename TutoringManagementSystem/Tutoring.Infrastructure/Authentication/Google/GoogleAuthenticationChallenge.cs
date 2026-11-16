namespace Tutoring.Infrastructure.Authentication.Google;

public sealed class GoogleAuthenticationChallenge(string nonce, DateTimeOffset expiresAtUtc)
{
    public string Nonce { get; } = nonce;
    public DateTimeOffset ExpiresAtUtc { get; } = expiresAtUtc;
}
