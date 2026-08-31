namespace Tutoring.Domain.Common.Exceptions;

public sealed class PasswordPolicyViolationException : UseCaseException
{
    public PasswordPolicyViolationException(string message)
        : base(
            "Password.PolicyViolation",
            message)
    {
    }
}
