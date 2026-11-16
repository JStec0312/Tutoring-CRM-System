using System.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Common.Exceptions;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication.Passwords;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Auth.ChangePassword;

public sealed class ChangePasswordHandler(
    TutoringDbContext db,
    IPasswordHasher hasher,
    IPasswordPolicyValidator policy,
    TimeProvider clock,
    ILogger<ChangePasswordHandler> logger) : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var user = await db.UserAccounts.SingleOrDefaultAsync(
            user => user.Id == request.AccountId, cancellationToken);
        if (user is null || !user.IsActive || !hasher.Verify(request.CurrentPassword, user.PasswordHash.Value))
            throw new IncorrectCurrentPasswordException();

        policy.Validate(request.NewPassword);
        var now = clock.GetUtcNow();
        user.ChangePassword(new PasswordHash(hasher.Hash(request.NewPassword)));

        var sessions = await db.RefreshTokens.Where(token =>
            token.UserAccountId == request.AccountId && token.RevokedAtUtc == null).ToListAsync(cancellationToken);
        foreach (var session in sessions)
            session.Revoke(now.UtcDateTime);

        var resets = await db.PasswordResetTokens.Where(token =>
            token.UserAccountId == request.AccountId && token.UsedAtUtc == null &&
            token.InvalidatedAtUtc == null).ToListAsync(cancellationToken);
        foreach (var reset in resets)
            reset.Invalidate(now);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Password changed. UserId: {UserId}, RevokedSessions: {RevokedSessions}",
            request.AccountId.Value, sessions.Count);
    }
}
