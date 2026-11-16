using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Authentication.RefreshTokens;

public interface IRefreshTokenGenerator 
{
    GeneratedRefreshToken Generate(UserAccount userAccount, Guid familyId, string? createdByIp = null, string? createdByUserAgent = null);
    string Hash(string token);
    
}