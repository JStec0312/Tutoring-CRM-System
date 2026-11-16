using MediatR;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Auth.CompleteGoogleAccountLink;

public sealed record CompleteGoogleAccountLinkCommand(UserAccountId AccountId, string ContinuationToken) : IRequest;
