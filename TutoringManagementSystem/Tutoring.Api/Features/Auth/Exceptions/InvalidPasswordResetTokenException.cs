using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Auth.Exceptions;

public sealed class InvalidPasswordResetTokenException
    : AuthException
{
    public InvalidPasswordResetTokenException()
        : base("Auth.InvalidPasswordResetToken","Password reset token is invalid or expired.")
    {
    }
}