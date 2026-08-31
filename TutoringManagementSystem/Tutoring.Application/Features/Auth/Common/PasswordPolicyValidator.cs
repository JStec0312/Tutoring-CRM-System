using Microsoft.Extensions.Options;
using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Infrastructure.Authentication;

public sealed class PasswordPolicyValidator(
    IOptions<PasswordPolicyOptions> passwordPolicyOptions
) : IPasswordPolicyValidator
{
    private readonly PasswordPolicyOptions _options = passwordPolicyOptions.Value;

    public void Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new PasswordPolicyViolationException("Password cannot be empty.");
        }

        if (password.Length < _options.MinimumLength)
        {
            throw new PasswordPolicyViolationException(
                $"Password must be at least {_options.MinimumLength} characters long.");
        }

        if (password.Length > _options.MaximumLength)
        {
            throw new PasswordPolicyViolationException(
                $"Password must be at most {_options.MaximumLength} characters long.");
        }

        if (_options.RequireUppercase && !password.Any(char.IsUpper))
        {
            throw new PasswordPolicyViolationException(
                "Password must contain at least one uppercase letter.");
        }

        if (_options.RequireLowercase && !password.Any(char.IsLower))
        {
            throw new PasswordPolicyViolationException(
                "Password must contain at least one lowercase letter.");
        }

        if (_options.RequireDigit && !password.Any(char.IsDigit))
        {
            throw new PasswordPolicyViolationException(
                "Password must contain at least one digit.");
        }

        if (_options.RequireSpecialCharacter &&
            !password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            throw new PasswordPolicyViolationException(
                "Password must contain at least one special character.");
        }
    }
}