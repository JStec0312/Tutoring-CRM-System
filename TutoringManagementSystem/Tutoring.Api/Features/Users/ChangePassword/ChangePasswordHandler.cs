using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Users.Exceptions;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Users.ChangePassword;

public sealed class ChangePasswordHandler(
    TutoringDbContext dbContext,
    IPasswordHasher passwordHasher,
    IPasswordPolicyValidator passwordPolicyValidator,
    TimeProvider timeProvider,
    ILogger<ChangePasswordHandler> logger)
    : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        var userAccount = await dbContext.UserAccounts
            .SingleOrDefaultAsync(
                user => user.Id == request.UserAccountId,
                cancellationToken);

        if (userAccount is null)
        {
            logger.LogWarning(
                "User account not found during password change. UserId: {UserId}, RequestMetadata: {RequestMetadata}",
                request.UserAccountId.Value,
                request.RequestMetadata);
            throw new UserAccountNotFoundException(
                request.UserAccountId.Value);
        }

        if (!passwordHasher.Verify(
                request.CurrentPassword,
                userAccount.PasswordHash.Value))
        {
            logger.LogWarning(
                "Password change rejected because current password is invalid. UserId: {UserId}, RequestMetadata: {RequestMetadata}",
                request.UserAccountId.Value,
                request.RequestMetadata);
            throw new InvalidCurrentPasswordException();
        }

        passwordPolicyValidator.Validate(request.NewPassword);

        var passwordHash = new PasswordHash(
            passwordHasher.Hash(request.NewPassword));

        userAccount.ChangePassword(passwordHash);

        var activeRefreshTokens = await dbContext.RefreshTokens
            .Where(refreshToken =>
                refreshToken.UserAccountId == request.UserAccountId &&
                refreshToken.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        var revokedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var refreshToken in activeRefreshTokens)
        {
            refreshToken.Revoke(revokedAtUtc);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Password changed. UserId: {UserId}, RevokedSessions: {RevokedSessions}, RequestMetadata: {RequestMetadata}",
            request.UserAccountId.Value,
            activeRefreshTokens.Count,
            request.RequestMetadata);
    }
}
