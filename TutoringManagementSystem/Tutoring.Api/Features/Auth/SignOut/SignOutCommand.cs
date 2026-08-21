using MediatR;
using Tutoring.Api.Features.Common.Http;

namespace Tutoring.Api.Features.Auth.SignOut;

public sealed record SignOutCommand(
    string? RefreshToken,
    RequestMetadata Metadata) : IRequest;
