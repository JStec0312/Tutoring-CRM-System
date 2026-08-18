namespace Tutoring.Infrastructure.Utils;

public sealed record JwtToken(string Value, DateTime ExpiresAtUtc);
