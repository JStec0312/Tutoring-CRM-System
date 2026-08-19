namespace Tutoring.Infrastructure.Authentication;

public sealed record JwtToken(string Value, DateTime ExpiresAtUtc);
