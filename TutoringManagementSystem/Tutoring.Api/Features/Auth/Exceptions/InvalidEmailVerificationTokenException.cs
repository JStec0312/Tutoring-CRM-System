using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Auth.Exceptions;

public class InvalidEmailVerificationTokenException : AuthException
{
    public InvalidEmailVerificationTokenException() : base("Auth.InvalidEmailVerificationToken", "The email verification token is invalid.")
    {
    }
}