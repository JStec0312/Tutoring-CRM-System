using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Auth.SignOutAll;

public sealed class SignOutAllHandler(
    TutoringDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<SignOutAllHandler> logger)
    : IRequestHandler<SignOutAllCommand>
{
    public async Task Handle(
        SignOutAllCommand request,
        CancellationToken cancellationToken)
    {
        var activeRefreshTokens = await dbContext.RefreshTokens
            .Where(refreshToken =>
                refreshToken.UserAccountId == request.UserAccountId &&
                refreshToken.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        if (activeRefreshTokens.Count == 0)
        {
            logger.LogInformation(
                "User signed out from all sessions. UserId: {UserId}, RevokedSessions: {RevokedSessions}, IP: {IpAddress}, TraceId: {TraceId}",
                request.UserAccountId.Value,
                0,
                request.Metadata.IpAddress,
                request.Metadata.TraceId);
            return;
        }

        var revokedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var refreshToken in activeRefreshTokens)
        {
            refreshToken.Revoke(revokedAtUtc);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "User signed out from all sessions. UserId: {UserId}, RevokedSessions: {RevokedSessions}, IP: {IpAddress}, TraceId: {TraceId}",
            request.UserAccountId.Value,
            activeRefreshTokens.Count,
            request.Metadata.IpAddress,
            request.Metadata.TraceId);
    }
}
