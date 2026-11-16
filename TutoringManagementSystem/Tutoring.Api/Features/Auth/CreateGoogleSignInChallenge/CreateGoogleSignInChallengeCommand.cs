using MediatR;

namespace Tutoring.Api.Features.Auth.CreateGoogleSignInChallenge;

public sealed record CreateGoogleSignInChallengeCommand : IRequest<CreateGoogleSignInChallengeResponse>;
