using MediatR;
using Tutoring.Api.Features.Common.Http;

namespace Tutoring.Api.Features.Auth.RefreshToken;

public sealed record RefreshTokenCommand(
    string RefreshToken,
    RequestMetadata Metadata
) : IRequest<RefreshTokenHandlerResult>;
