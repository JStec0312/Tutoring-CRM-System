namespace Tutoring.Api.Features.Auth.RefreshToken;

public sealed record RefreshTokenResponse(
    string AccessToken,
    DateTime ExpiresAt
);
