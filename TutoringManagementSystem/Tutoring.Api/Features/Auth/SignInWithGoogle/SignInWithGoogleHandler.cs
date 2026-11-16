using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Auth.Exceptions;
using Tutoring.Api.Features.Auth.Login;
using Tutoring.Api.Features.Common.Exceptions;
using Tutoring.Domain.Common;
using Tutoring.Domain.Common.Exceptions;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication.Google;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Auth.SignInWithGoogle;

public sealed class SignInWithGoogleHandler(
    TutoringDbContext dbContext,
    IGoogleSignInTokenVerifier tokenVerifier,
    LoginSessionIssuer sessionIssuer,
    TimeProvider timeProvider)
    : IRequestHandler<SignInWithGoogleCommand, SignInWithGoogleResult>
{
    public async Task<SignInWithGoogleResult> Handle(
        SignInWithGoogleCommand request, CancellationToken cancellationToken)
    {
        // Is the token valid and not expired?
        var identity = await tokenVerifier.VerifyAsync(request.IdToken, cancellationToken);
        var now = timeProvider.GetUtcNow();

        // Begin a database transaction to ensure atomicity of the sign-in process.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        // Consume the Google sign-in challenge to prevent replay attacks.
        var consumed = await dbContext.GoogleAuthenticationChallenges
            .Where(challenge => challenge.Nonce == identity.Nonce && challenge.ExpiresAtUtc > now)
            .ExecuteDeleteAsync(cancellationToken);

        // Check if there is an existing account linked to the Google identity.
        if (consumed == 0)
            throw new AuthException("Auth.InvalidGoogleSignIn", "The Google sign-in challenge is invalid or expired.");
    
        var account = await dbContext.UserAccounts
            .AsNoTracking()
            .Include(user => user.RoleAssignments)
            .SingleOrDefaultAsync(user => dbContext.GoogleSignInAccountLinks.Any(link =>
                link.AccountId == user.Id && link.GoogleSubject == identity.Subject), cancellationToken);

        SignInWithGoogleResult result;
        // Determine the result based on whether an account was found.
        // found - sign the user in.
        if (account is not null)
        {
            if (!account.IsActive)
                throw new InactiveAccountException();
            // Issue a session for the signed-in user.
            result = new SignInWithGoogleResult.SignedIn(sessionIssuer.Issue(account, request.Metadata));
        }
        else
        {
            // No linked account found - create a continuation for the sign-in process.
            result = await CreateContinuationAsync(identity, now, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<SignInWithGoogleResult.Continue> CreateContinuationAsync(
        VerifiedGoogleSignInIdentity identity, DateTimeOffset now, CancellationToken cancellationToken)
    {

        // Email address verified by Google is required for continuation.
        // if the account exists, it must be active to continue the sign-in process.
        var email = GetVerifiedEmail(identity.VerifiedEmail);
        var account = await dbContext.UserAccounts.AsNoTracking()
            .Where(user => user.Email.Value == email)
            .Select(user => new { user.Id, user.Status })
            .SingleOrDefaultAsync(cancellationToken);
        if (account is not null && account.Status != AccountStatus.Active)
            throw new InactiveAccountException();

        // Clean up any expired Google sign-in continuations.
        await dbContext.GoogleSignInContinuations
            .Where(continuation => continuation.ExpiresAtUtc <= now)
            .ExecuteDeleteAsync(cancellationToken);

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiresAtUtc = now.AddMinutes(5);
        // Create a new Google sign-in continuation with a short expiration time.
        dbContext.GoogleSignInContinuations.Add(new GoogleSignInContinuation(
            GoogleSignInContinuation.HashToken(token), identity.Subject, email,
            account?.Id, expiresAtUtc));
        // Return the continuation result to the caller. (requires link if account exists)
        return new SignInWithGoogleResult.Continue(account is not null, token, expiresAtUtc);
    }

    private static string GetVerifiedEmail(string? email)
    {
        if (email is not { Length: > 0 and <= 320 })
            throw new AuthException("Auth.GoogleVerifiedEmailRequired", "Google did not provide a verified email address.");

        try
        {
            return new EmailAddress(email).Value;
        }
        catch (InvalidEmailAddressException)
        {
            throw new AuthException("Auth.GoogleVerifiedEmailRequired", "Google did not provide a valid verified email address.");
        }
    }
}
