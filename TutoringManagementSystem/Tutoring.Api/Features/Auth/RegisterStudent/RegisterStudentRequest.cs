namespace Tutoring.Api.Features.Auth.RegisterStudent;

public sealed record RegisterStudentRequest(
    string Email,
    string Password,
    string UserName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber
);
