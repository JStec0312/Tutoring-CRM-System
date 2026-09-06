using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Authentication;

public interface IEmailVerificationTokenGenerator
{
    GeneratedEmailVerificationToken Generate(
        UserAccountId userAccountId,
        DateTimeOffset now);
}