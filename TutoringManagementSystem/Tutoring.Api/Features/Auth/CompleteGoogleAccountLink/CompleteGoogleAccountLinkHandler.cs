using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Auth.Exceptions;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication.Google;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Auth.CompleteGoogleAccountLink;

public sealed class CompleteGoogleAccountLinkHandler(TutoringDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<CompleteGoogleAccountLinkCommand>
{
    public async Task Handle(CompleteGoogleAccountLinkCommand request, CancellationToken cancellationToken)
    {
        // take the token
        var tokenHash = GoogleSignInContinuation.HashToken(request.ContinuationToken);
        
        var now = timeProvider.GetUtcNow();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        // find the continuation record for the given token and account ID that has not expired
        var continuation = await dbContext.GoogleSignInContinuations
            .AsNoTracking()
            .SingleOrDefaultAsync(link =>
                link.TokenHash == tokenHash &&
                link.AccountId == request.AccountId &&
                link.ExpiresAtUtc > now, cancellationToken);

        if (continuation is null)
        {
            throw new InvalidGoogleLinkContinuationException();
        }

        var status = await dbContext.UserAccounts
            .Where(account => account.Id == request.AccountId && account.Email.Value == continuation.Email)
            .Select(account => (AccountStatus?)account.Status)
            .SingleOrDefaultAsync(cancellationToken);
        if (status is null)
            throw new InvalidGoogleLinkContinuationException();
        if (status != AccountStatus.Active)
            throw new InactiveAccountException();

        // delete the continuation record to prevent reuse
        var deletedCount = await dbContext.GoogleSignInContinuations
            .Where(link => link.TokenHash == tokenHash)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCount != 1)
        {
            throw new InvalidGoogleLinkContinuationException();
        }
        // create the link between the Google account and the user account
        dbContext.GoogleSignInAccountLinks.Add(new GoogleSignInAccountLink(continuation.GoogleSubject, request.AccountId));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new GoogleAccountLinkConflictException();
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
