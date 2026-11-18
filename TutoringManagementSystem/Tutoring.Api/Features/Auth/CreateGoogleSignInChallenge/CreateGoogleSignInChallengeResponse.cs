namespace Tutoring.Api.Features.Auth.CreateGoogleSignInChallenge;

public sealed record CreateGoogleSignInChallengeResponse(string Nonce, DateTimeOffset ExpiresAtUtc);
