namespace Tutoring.Api.Features.Auth.RefreshToken;

public sealed record RefreshTokenHandlerResult(
    string AccessToken,
    DateTime ExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt
);