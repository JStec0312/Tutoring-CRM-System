using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Utils;
namespace Tutoring.Infrastructure.Abstraction;

public interface IJwtTokenGenerator
{
    JwtToken Generate(UserAccount userAccount);
}