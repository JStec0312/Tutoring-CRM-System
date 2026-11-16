using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Authentication.EmailVerification;

public interface IEmailVerificationTokenGenerator
{
    GeneratedEmailVerificationToken Generate(
        UserAccountId userAccountId,
        DateTimeOffset now);
}