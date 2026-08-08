namespace Tutoring.Api.Features.Auth;

public sealed record JwtToken(
    string Value,
    DateTime ExpiresAtUtc
);