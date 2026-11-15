using System.Security.Cryptography;
using MediatR;
using Tutoring.Infrastructure.Authentication;
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
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiresAt = clock.GetUtcNow().AddMinutes(5);

        db.GoogleSignInChallenges.Add(
            new GoogleSignInChallenge(nonce, request.AccountId.Value, expiresAt));

        await db.SaveChangesAsync(cancellationToken);

        return new CreateGoogleChallengeResponse(nonce);
    }
}