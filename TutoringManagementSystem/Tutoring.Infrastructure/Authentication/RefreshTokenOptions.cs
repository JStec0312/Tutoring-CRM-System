namespace Tutoring.Infrastructure.Authentication;

public sealed class RefreshTokenOptions
{
    public const string SectionName = "RefreshToken";

    //  "RefreshTokenExpirationDays": 30,
    // "RefreshTokenCookieName": "refresh_token",
    // "HttpOnlyRefreshTokenCookie": true,
    // "SameSiteRefreshTokenCookie": "Strict",
    // "SecureRefreshTokenCookie": true,
    // "RefreshTokenPath": "/api/auth/"

    public int RefreshTokenExpirationDays { get; init; } = 30;
    public string RefreshTokenCookieName { get; init; } = "refresh_token";
    public bool HttpOnlyRefreshTokenCookie { get; init; } = true;
    public string SameSiteRefreshTokenCookie { get; init; } = "Strict";
    public bool SecureRefreshTokenCookie { get; init; } = true;
    public string RefreshTokenPath { get; init; } = "/api/auth/";
}