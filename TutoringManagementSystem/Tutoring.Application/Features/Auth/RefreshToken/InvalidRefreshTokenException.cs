namespace Tutoring.Api.Features.Auth.RefreshToken;
using Tutoring.Api.Features.Common.Exceptions;
public class InvalidRefreshTokenException : AuthException
{
    public InvalidRefreshTokenException() : base("Auth.InvalidRefreshToken", "Refresh token is invalid or expired.")
    {
    }
}