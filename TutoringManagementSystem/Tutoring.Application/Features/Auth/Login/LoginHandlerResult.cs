namespace Tutoring.Api.Features.Auth.Login;

public record LoginHandlerResult(
    string AccessToken,
    DateTime ExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt
);