using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Authentication;

public class RefreshTokenGenerator(IOptions<RefreshTokenOptions> options) : IRefreshTokenGenerator
{
    public RefreshTokenOptions refreshTokenOptions => options.Value;
    public GeneratedRefreshToken Generate(UserAccount userAccount, Guid familyId, string? CreatedByIp = null, string? CreatedByUserAgent = null)
    {
        var tokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expiresAt = DateTime.UtcNow.AddDays(refreshTokenOptions.RefreshTokenExpirationDays);
        var dataBaseToken = new RefreshToken(
            userAccount.Id,
             Hash(tokenValue),
             familyId,
            expiresAt,
            CreatedByIp ?? string.Empty,
            CreatedByUserAgent ?? string.Empty
        );
        return new GeneratedRefreshToken(dataBaseToken, tokenValue);
    }



    public string Hash(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}