using MediatR;
using Tutoring.Api.Features.Common.Http;

namespace Tutoring.Api.Features.Auth.RegisterStudent;

public sealed record RegisterStudentCommand(
    string Email,
    string Password,
    string UserName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    RequestMetadata Metadata
) : IRequest<RegisterStudentResponse>;
