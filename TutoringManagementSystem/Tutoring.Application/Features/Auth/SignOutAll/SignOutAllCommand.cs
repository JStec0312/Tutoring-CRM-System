using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Auth.SignOutAll;

public sealed record SignOutAllCommand(
    UserAccountId UserAccountId,
    RequestMetadata Metadata) : IRequest;
