using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Auth.Exceptions;

public class PasswordPolicyViolationException : UseCaseException
{
    public PasswordPolicyViolationException(string message)
        : base(
            "Password.PolicyViolation",
            message)
    {
    }

}