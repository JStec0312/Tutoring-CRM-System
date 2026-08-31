namespace Tutoring.Api.Features.Auth.RegisterTutor;

public sealed record RegisterTutorRequest(
    string Email,
    string Password,
    string UserName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber
);
