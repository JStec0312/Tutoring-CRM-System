using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Auth.Exceptions;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication.Google;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Auth.CreateGoogleChallenge;

public sealed class CreateGoogleChallengeHandler(
    TutoringDbContext db,
    TimeProvider clock)
    : IRequestHandler<CreateGoogleChallengeCommand, CreateGoogleChallengeResponse>
{
    public async Task<CreateGoogleChallengeResponse> Handle(
        CreateGoogleChallengeCommand request,
        CancellationToken cancellationToken)
    {
        var activeAccount = await db.UserAccounts.AnyAsync(account =>
            account.Id == request.AccountId && account.Status == AccountStatus.Active,
            cancellationToken);
        if (!activeAccount)
        {
            throw new InactiveAccountException();
        }

        var now = clock.GetUtcNow();
        await db.GoogleSignInChallenges
            .Where(challenge => challenge.ExpiresAtUtc <= now)
            .ExecuteDeleteAsync(cancellationToken);

        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiresAt = now.AddMinutes(5);

        db.GoogleSignInChallenges.Add(
            new GoogleSignInChallenge(nonce, request.AccountId.Value, expiresAt));

        await db.SaveChangesAsync(cancellationToken);

        return new CreateGoogleChallengeResponse(nonce);
    }
}