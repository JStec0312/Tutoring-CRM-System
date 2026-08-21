using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Authentication;

public class RefreshTokenGenerator(
    IOptions<RefreshTokenOptions> options,
    TimeProvider timeProvider) : IRefreshTokenGenerator
{
    public RefreshTokenOptions refreshTokenOptions => options.Value;
    public GeneratedRefreshToken Generate(UserAccount userAccount, Guid familyId, string? createdByIp = null, string? createdByUserAgent = null)
    {
        var tokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var createdAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = createdAtUtc.AddDays(refreshTokenOptions.RefreshTokenExpirationDays);
        var dataBaseToken = new RefreshToken(
            userAccount.Id,
            Hash(tokenValue),
            familyId,
            createdAtUtc,
            expiresAt,
            createdByIp ?? string.Empty,
            createdByUserAgent ?? string.Empty
        );
        return new GeneratedRefreshToken(dataBaseToken, tokenValue);
    }



    public string Hash(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}