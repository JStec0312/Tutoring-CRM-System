using MediatR;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Users.GetProfile;

public sealed record GetProfileQuery(UserAccountId AccountId) : IRequest<ProfileResponse>;
