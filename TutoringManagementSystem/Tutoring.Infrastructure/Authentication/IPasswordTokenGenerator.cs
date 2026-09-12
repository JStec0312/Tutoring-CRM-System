using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Authentication;

public interface IPasswordResetTokenGenerator
{
    GeneratedPasswordResetToken Generate(
        UserAccountId userAccountId,
        DateTimeOffset now);
}