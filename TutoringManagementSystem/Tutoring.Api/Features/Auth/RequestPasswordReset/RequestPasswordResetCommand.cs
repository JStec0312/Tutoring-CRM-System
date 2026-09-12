using MediatR;
using Tutoring.Api.Features.Common.Http;

namespace Tutoring.Api.Features.Auth.RequestPasswordReset;

public sealed record RequestPasswordResetCommand(
    string Email,
    RequestMetadata Metadata)
    : IRequest;