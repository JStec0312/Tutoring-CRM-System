using MediatR;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Auth.LinkGoogleAccount;

public sealed record LinkGoogleAccountCommand(
    UserAccountId AccountId,
    string IdToken) : IRequest;
