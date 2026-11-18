using Tutoring.Infrastructure.Authentication.RefreshTokens;

namespace Tutoring.Api.Features.Auth.Login;

internal static class LoginRefreshTokenCookie
{
    public static void Append(HttpResponse response, RefreshTokenOptions options, LoginHandlerResult session)
    {
        response.Cookies.Append(options.RefreshTokenCookieName, session.RefreshToken, new CookieOptions
        {
            HttpOnly = options.HttpOnlyRefreshTokenCookie,
            SameSite = options.SameSiteRefreshTokenCookie switch
            {
                "Strict" => SameSiteMode.Strict,
                "Lax" => SameSiteMode.Lax,
                "None" => SameSiteMode.None,
                _ => throw new InvalidOperationException($"Invalid SameSite value: {options.SameSiteRefreshTokenCookie}")
            },
            Secure = options.SecureRefreshTokenCookie,
            Expires = new DateTimeOffset(session.RefreshTokenExpiresAt, TimeSpan.Zero),
            Path = options.RefreshTokenPath
        });
    }
}
