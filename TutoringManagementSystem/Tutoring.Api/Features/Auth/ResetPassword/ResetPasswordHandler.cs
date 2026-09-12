using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Auth.Exceptions;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Auth.ResetPassword;

public sealed class ResetPasswordHandler(
    TutoringDbContext dbContext,
    IPasswordHasher passwordHasher,
    IPasswordPolicyValidator passwordPolicyValidator,
    TimeProvider timeProvider,
    ILogger<ResetPasswordHandler> logger)
    : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    request.Token)));

        var passwordResetToken =
            await dbContext.PasswordResetTokens
                .SingleOrDefaultAsync(
                    token =>
                        token.TokenHash == tokenHash,
                    cancellationToken);

        var now = timeProvider.GetUtcNow();

        if (passwordResetToken is null ||
            passwordResetToken.IsUsed ||
            passwordResetToken.IsInvalidated ||
            passwordResetToken.ExpiresAtUtc <= now)
        {
            logger.LogWarning(
                "Invalid password reset token. RequestMetadata: {RequestMetadata}",
                request.Metadata);

            throw new InvalidPasswordResetTokenException();
        }

        var userAccount =
            await dbContext.UserAccounts
                .SingleOrDefaultAsync(
                    user =>
                        user.Id ==
                        passwordResetToken.UserAccountId,
                    cancellationToken);

        if (userAccount is null)
        {
            logger.LogWarning(
                "User for password reset token not found. UserId: {UserId}, RequestMetadata: {RequestMetadata}",
                passwordResetToken.UserAccountId.Value,
                request.Metadata);

            throw new InvalidPasswordResetTokenException();
        }


        passwordPolicyValidator.Validate(
            request.NewPassword);

        var passwordHash =
            new PasswordHash(
                passwordHasher.Hash(
                    request.NewPassword));

        userAccount.ChangePassword(
            passwordHash);

        passwordResetToken.MarkAsUsed(now);

        /*logging out from all devices after password reset*/

        var activeRefreshTokens =
            await dbContext.RefreshTokens
                .Where(refreshToken =>
                    refreshToken.UserAccountId ==
                        userAccount.Id &&
                    refreshToken.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

        var revokedAtUtc = now.UtcDateTime;

        foreach (var refreshToken in activeRefreshTokens)
        {
            refreshToken.Revoke(revokedAtUtc);
        }

        // Killing all other active password reset tokens for this user
        var otherPasswordResetTokens =
            await dbContext.PasswordResetTokens
                .Where(token =>
                    token.UserAccountId == userAccount.Id &&
                    token.Id != passwordResetToken.Id &&
                    token.UsedAtUtc == null &&
                    token.InvalidatedAtUtc == null)
                .ToListAsync(cancellationToken);

        foreach (var token in otherPasswordResetTokens)
        {
            token.Invalidate(now);
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        logger.LogInformation(
            "Password reset succeeded. UserId: {UserId}, RevokedSessions: {RevokedSessions}, RequestMetadata: {RequestMetadata}",
            userAccount.Id.Value,
            activeRefreshTokens.Count,
            request.Metadata);
    }
}