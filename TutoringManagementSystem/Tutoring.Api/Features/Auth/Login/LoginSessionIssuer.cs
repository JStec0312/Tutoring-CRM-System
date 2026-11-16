using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication.Jwt;
using Tutoring.Infrastructure.Authentication.RefreshTokens;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Auth.Login;

public sealed class LoginSessionIssuer(
    TutoringDbContext dbContext, IJwtTokenGenerator jwtTokenGenerator, IRefreshTokenGenerator refreshTokenGenerator)
{
    public LoginHandlerResult Issue(UserAccount userAccount, RequestMetadata metadata)
    {
        var accessToken = jwtTokenGenerator.Generate(userAccount);
        var refreshToken = refreshTokenGenerator.Generate(
            userAccount, Guid.NewGuid(), metadata.IpAddress, metadata.UserAgent);
        dbContext.RefreshTokens.Add(refreshToken.Token);

        return new LoginHandlerResult(
            accessToken.Value, accessToken.ExpiresAtUtc, refreshToken.Value, refreshToken.Token.ExpiresAtUtc);
    }
}
