using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Authentication;

public class JwtTokenGenerator (
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider
    ): IJwtTokenGenerator
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    public JwtToken Generate(UserAccount userAccount)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAtUtc = now.AddMinutes(_jwtOptions.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(
                "sub",
                userAccount.Id.Value.ToString()),

            new(
                "email",
                userAccount.Email.Value),

            new(
                "jti",
                Guid.NewGuid().ToString())
        };

        claims.AddRange(userAccount.Roles.Select(role => new Claim("role", role.ToString())));
        var signingKey  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));

        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),

            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,

            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAtUtc,

            SigningCredentials = credentials
        };
        var tokenHandler = new JsonWebTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return new JwtToken(token, expiresAtUtc);
    }
}