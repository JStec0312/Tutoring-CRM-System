namespace Tutoring.Infrastructure.Authentication;

public interface IPasswordPolicyValidator
{
    void Validate(string? password);
}
