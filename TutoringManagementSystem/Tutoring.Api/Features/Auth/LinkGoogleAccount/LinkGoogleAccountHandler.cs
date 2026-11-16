using System.Data;
using Google.Apis.Auth;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Auth.Exceptions;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication.Google;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Auth.LinkGoogleAccount;

public sealed class LinkGoogleAccountHandler(
    IGoogleIdTokenVerifier verifier,
    TutoringDbContext db,
    TimeProvider clock,
    ILogger<LinkGoogleAccountHandler> logger) : IRequestHandler<LinkGoogleAccountCommand>
{
    public async Task Handle(LinkGoogleAccountCommand request, CancellationToken cancellationToken)
    {
        VerifiedGoogleIdentity google;
        try
        {
            google = await verifier.VerifyAsync(request.IdToken, cancellationToken);
        }
        catch (InvalidJwtException)
        {
            throw new InvalidGoogleLinkCredentialException();
        }

        var accountId = request.AccountId.Value;
        var now = clock.GetUtcNow();
        var consumed = await db.GoogleSignInChallenges
            .Where(challenge => challenge.Nonce == google.Nonce &&
                challenge.AccountId == accountId &&
                challenge.ExpiresAtUtc > now)
            .ExecuteDeleteAsync(cancellationToken);
        if (consumed != 1)
        {
            throw new InvalidGoogleLinkCredentialException();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);
        var accountIsActive = await db.UserAccounts.AnyAsync(
            user => user.Id == request.AccountId && user.Status == AccountStatus.Active,
            cancellationToken);
        if (!accountIsActive)
        {
            throw new InactiveAccountException();
        }

        db.GoogleAccountLinks.Add(new GoogleAccountLink(request.AccountId, google.Subject));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new GoogleAccountLinkConflictException();
        }

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Google account linked. UserId: {UserId}", accountId);
    }
}
