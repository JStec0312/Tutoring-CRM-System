using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Auth.RefreshToken;

public sealed class RefreshTokenHandler(
    TutoringDbContext dbContext,
    IRefreshTokenGenerator refreshTokenGenerator,
    IJwtTokenGenerator jwtTokenGenerator,
    TimeProvider timeProvider,
    ILogger<RefreshTokenHandler> logger
) : IRequestHandler<RefreshTokenCommand, RefreshTokenHandlerResult>
{
    public async Task<RefreshTokenHandlerResult> Handle(RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var refreshTokenHash = refreshTokenGenerator.Hash(request.RefreshToken);
        var storedRefreshToken = await dbContext.RefreshTokens.SingleOrDefaultAsync(
            rt => rt.TokenHash == refreshTokenHash,
            cancellationToken
        );
        if (storedRefreshToken is null)
        {
            logger.LogWarning(
                "Refresh token rejected. UserId: {UserId}, FamilyId: {FamilyId}, IP: {IpAddress}",
                "Unknown",
                "Unknown",
                request.Metadata.IpAddress);
            throw new InvalidRefreshTokenException();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (storedRefreshToken.IsRevoked || storedRefreshToken.IsExpired(now))
        {
            logger.LogWarning(
                "Refresh token rejected. UserId: {UserId}, FamilyId: {FamilyId}, IP: {IpAddress}",
                storedRefreshToken.UserAccountId.Value,
                storedRefreshToken.FamilyId,
                request.Metadata.IpAddress);
            // revoke old refresh token (hacker might be trying to reuse it)
            var familyTokens = await dbContext.RefreshTokens
            .Where(rt =>
                rt.FamilyId == storedRefreshToken.FamilyId &&
                rt.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in familyTokens)
        {
            token.Revoke(now);
        }
            throw new InvalidRefreshTokenException();
        }


        var userAccount = await dbContext.UserAccounts
            .Include(user => user.RoleAssignments)
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user =>
                    user.Id == storedRefreshToken.UserAccountId,
                cancellationToken);

        if (userAccount is null ||
            userAccount.Status != AccountStatus.Active)
        {
            logger.LogWarning(
                "Refresh token rejected. UserId: {UserId}, FamilyId: {FamilyId}, IP: {IpAddress}",
                storedRefreshToken.UserAccountId.Value,
                storedRefreshToken.FamilyId,
                request.Metadata.IpAddress);
            throw new InvalidRefreshTokenException();
        }

        // new access token
        var accessToken = jwtTokenGenerator.Generate(userAccount);
        // new refresh token  same family id
        var generatedRefreshToken = refreshTokenGenerator.Generate(
            userAccount,
            storedRefreshToken.FamilyId,
            request.Metadata.IpAddress,
            request.Metadata.UserAgent);

        // revoke old refresh token
        
        storedRefreshToken.Revoke(now, generatedRefreshToken.Token.Id);
        dbContext.RefreshTokens.Add(generatedRefreshToken.Token);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Refresh token accepted. UserId: {UserId}, FamilyId: {FamilyId}, IP: {IpAddress}",
            storedRefreshToken.UserAccountId.Value,
            storedRefreshToken.FamilyId,
            request.Metadata.IpAddress);
        return new RefreshTokenHandlerResult(
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            generatedRefreshToken.Value,
            generatedRefreshToken.Token.ExpiresAtUtc
        );

    }
}