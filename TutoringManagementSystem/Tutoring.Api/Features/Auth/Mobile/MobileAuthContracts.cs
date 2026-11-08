namespace Tutoring.Api.Features.Auth.Mobile;

public static class MobileAuthRoutes
{
    public const string Login = "api/auth/mobile/login";
    public const string Refresh = "api/auth/mobile/refresh";
    public const string SignOut = "api/auth/mobile/sign-out";
}

public sealed record MobileSessionResponse(
    string AccessToken,
    DateTime ExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);

public sealed record MobileRefreshTokenRequest(string RefreshToken);
