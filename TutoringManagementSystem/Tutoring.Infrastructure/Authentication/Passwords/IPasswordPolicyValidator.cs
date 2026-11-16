namespace Tutoring.Infrastructure.Authentication.Passwords;

public interface IPasswordPolicyValidator
{
    void Validate(string? password);
}
