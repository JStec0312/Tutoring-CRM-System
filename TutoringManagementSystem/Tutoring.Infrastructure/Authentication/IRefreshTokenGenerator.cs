using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Authentication;

public interface IRefreshTokenGenerator 
{
    GeneratedRefreshToken Generate(UserAccount userAccount, Guid familyId, string? CreatedByIp = null, string? CreatedByUserAgent = null);
    string Hash(string token);
    
}