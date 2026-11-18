using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Infrastructure.Authentication.Google;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Auth.CreateGoogleSignInChallenge;

public sealed class CreateGoogleSignInChallengeHandler(TutoringDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<CreateGoogleSignInChallengeCommand, CreateGoogleSignInChallengeResponse>
{
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);

    public async Task<CreateGoogleSignInChallengeResponse> Handle(
        CreateGoogleSignInChallengeCommand request,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiresAtUtc = now.Add(ChallengeLifetime);

        await dbContext.GoogleAuthenticationChallenges
            .Where(challenge => challenge.ExpiresAtUtc <= now)
            .ExecuteDeleteAsync(cancellationToken);

        dbContext.GoogleAuthenticationChallenges.Add(new GoogleAuthenticationChallenge(nonce, expiresAtUtc));
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateGoogleSignInChallengeResponse(nonce, expiresAtUtc);
    }
}
