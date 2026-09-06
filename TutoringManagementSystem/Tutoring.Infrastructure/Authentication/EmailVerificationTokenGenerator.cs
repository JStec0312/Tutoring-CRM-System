namespace Tutoring.Infrastructure.Authentication;

using System.Security.Cryptography;
using Tutoring.Domain.Identity;

public sealed class EmailVerificationTokenGenerator
    : IEmailVerificationTokenGenerator
{
    public GeneratedEmailVerificationToken Generate(
        UserAccountId userAccountId,
        DateTimeOffset now)
    {
        var value = Convert.ToHexString(
            RandomNumberGenerator.GetBytes(32));

        var hash = Convert.ToHexString(
            SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(value)));

        var token = new EmailVerificationToken(
            userAccountId,
            hash,
            now.AddHours(24));

        return new GeneratedEmailVerificationToken(
            value,
            token);
    }
}