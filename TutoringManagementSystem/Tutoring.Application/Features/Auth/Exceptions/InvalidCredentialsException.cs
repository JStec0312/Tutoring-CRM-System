using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Auth.Exceptions;

public sealed class InvalidCredentialsException : AuthException
{
    public InvalidCredentialsException() : base("Auth.InvalidCredentials", "Invalid credentials provided.")
    {
    }
}