using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Authentication;

public sealed class PasswordResetTokenGenerator(
    IOptions<PasswordResetOptions> options)
    : IPasswordResetTokenGenerator
{
    public GeneratedPasswordResetToken Generate(
        UserAccountId userAccountId,
        DateTimeOffset now)
    {
        var value = Convert.ToHexString(
            RandomNumberGenerator.GetBytes(32));

        var hash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(value)));

        var token = new PasswordResetToken(
            userAccountId,
            hash,
            now.AddHours(
                options.Value.ResetUrlActiveHours));

        return new GeneratedPasswordResetToken(
            value,
            token);
    }
}