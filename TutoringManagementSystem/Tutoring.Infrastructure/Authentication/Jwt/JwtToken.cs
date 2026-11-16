namespace Tutoring.Infrastructure.Authentication.Jwt;

public sealed record JwtToken(string Value, DateTime ExpiresAtUtc);
