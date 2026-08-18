using MediatR;

namespace Tutoring.Api.Features.Auth.RegisterTutor;

public sealed record RegisterTutorCommand(
    string Email,
    string Password,
    string UserName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber
) : IRequest<RegisterTutorResponse>;
