using MediatR;

namespace Tutoring.Api.Features.Auth.RegisterStudent;

public sealed record RegisterStudentCommand(
    string Email,
    string Password,
    string UserName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber
) : IRequest<RegisterStudentResponse>;
 