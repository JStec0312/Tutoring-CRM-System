using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Auth.Exceptions;
using Tutoring.Infrastructure.Authentication;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Auth.Login;

public sealed class LoginHandler(
    TutoringDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IRefreshTokenGenerator refreshTokenGenerator,
    ILogger<LoginHandler> logger)
    : IRequestHandler<LoginCommand, LoginHandlerResult>
{
    public async Task<LoginHandlerResult> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        var userAccount = await dbContext.UserAccounts
        .Include(user => user.RoleAssignments)
        .AsNoTracking()
        .SingleOrDefaultAsync(
        user => user.Email.Value == email,
        cancellationToken);

        if (userAccount is null)
        {
            logger.LogWarning("Login failed for email: {Email} - user not found", email);
            throw new InvalidCredentialsException();
        }

        var passwordIsValid = passwordHasher.Verify(
            request.Password,
            userAccount.PasswordHash.Value);

        if (!passwordIsValid)
        {
            logger.LogWarning("Login failed for email: {Email} - invalid password", email);
            throw new InvalidCredentialsException();
        }
        if(!userAccount.IsActive)
        {
            logger.LogWarning("Login failed for email: {Email} - account is disabled", email);
            throw new InactiveAccountException();
        }
        var accessToken = jwtTokenGenerator.Generate(userAccount);

        var familyId = Guid.NewGuid();
        var CreatedByIp = request.Metadata.IpAddress;
        var CreatedByUserAgent = request.Metadata.UserAgent;
        var generatedRefreshToken = refreshTokenGenerator.Generate(userAccount, familyId, CreatedByIp, CreatedByUserAgent);
        dbContext.RefreshTokens.Add(generatedRefreshToken.Token);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new LoginHandlerResult(
            AccessToken: accessToken.Value,
            ExpiresAt: accessToken.ExpiresAtUtc,
            RefreshToken: generatedRefreshToken.Value,
            RefreshTokenExpiresAt: generatedRefreshToken.Token.ExpiresAtUtc);
    }
}