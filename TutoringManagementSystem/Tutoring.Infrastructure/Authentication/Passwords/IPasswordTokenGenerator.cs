using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Authentication.Passwords;

public interface IPasswordResetTokenGenerator
{
    GeneratedPasswordResetToken Generate(
        UserAccountId userAccountId,
        DateTimeOffset now);
}