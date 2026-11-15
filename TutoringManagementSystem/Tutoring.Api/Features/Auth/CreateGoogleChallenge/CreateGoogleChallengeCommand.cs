using MediatR;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Auth.CreateGoogleChallenge;

public sealed record CreateGoogleChallengeCommand(UserAccountId AccountId)
    : IRequest<CreateGoogleChallengeResponse>;

