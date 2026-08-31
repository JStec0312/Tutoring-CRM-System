using MediatR;
using Tutoring.Api.Features.Common.Http;

namespace Tutoring.Api.Features.Auth.RegisterTutor;

public sealed record RegisterTutorCommand(
    string Email,
    string Password,
    string UserName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    RequestMetadata Metadata
) : IRequest<RegisterTutorResponse>;
