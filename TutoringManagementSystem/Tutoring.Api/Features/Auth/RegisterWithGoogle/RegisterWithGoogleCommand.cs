using MediatR;
using Tutoring.Api.Features.Auth.Login;
using Tutoring.Api.Features.Common.Http;

namespace Tutoring.Api.Features.Auth.RegisterWithGoogle;

public sealed record RegisterWithGoogleCommand(
    string ContinuationToken,
    string Role,
    string UserName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    RequestMetadata Metadata) : IRequest<LoginHandlerResult>;
