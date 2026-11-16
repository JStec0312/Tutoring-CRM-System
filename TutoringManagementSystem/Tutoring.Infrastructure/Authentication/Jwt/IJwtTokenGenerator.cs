using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Authentication.Jwt;

public interface IJwtTokenGenerator
{
    JwtToken Generate(UserAccount userAccount);
}
