using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Infrastructure.Authentication;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Auth.SignOut;

public sealed class SignOutHandler(
    TutoringDbContext dbContext,
    IRefreshTokenGenerator refreshTokenGenerator,
    TimeProvider timeProvider,
    ILogger<SignOutHandler> logger)
    : IRequestHandler<SignOutCommand>
{
    public async Task Handle(
        SignOutCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            logger.LogInformation(
                "Sign out request completed without a refresh token. IP: {IpAddress}, TraceId: {TraceId}",
                request.Metadata.IpAddress,
                request.Metadata.TraceId);
            return;
        }

        var refreshTokenHash = refreshTokenGenerator.Hash(request.RefreshToken);
        var storedRefreshToken = await dbContext.RefreshTokens.SingleOrDefaultAsync(
            refreshToken => refreshToken.TokenHash == refreshTokenHash,
            cancellationToken);

        if (storedRefreshToken is null || storedRefreshToken.IsRevoked)
        {
            logger.LogInformation(
                "Sign out request completed. IP: {IpAddress}, TraceId: {TraceId}",
                request.Metadata.IpAddress,
                request.Metadata.TraceId);
            return;
        }

        var revokedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        storedRefreshToken.Revoke(revokedAtUtc);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "User signed out. UserId: {UserId}, IP: {IpAddress}, TraceId: {TraceId}",
            storedRefreshToken.UserAccountId.Value,
            request.Metadata.IpAddress,
            request.Metadata.TraceId);
    }
}
