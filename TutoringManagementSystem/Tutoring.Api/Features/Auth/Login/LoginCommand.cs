using MediatR;
using Tutoring.Api.Features.Common.Http;

namespace Tutoring.Api.Features.Auth.Login;

public sealed record LoginCommand(
    string Email,
    string Password,
    RequestMetadata Metadata) : IRequest<LoginHandlerResult>;
