using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Api.Features.Common.Exceptions;

public class AuthException : UseCaseException
{
    public AuthException(string code, string message) : base(code, message)
    {
    }
}