using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Authentication;

public interface IJwtTokenGenerator
{
    JwtToken Generate(UserAccount userAccount);
}
