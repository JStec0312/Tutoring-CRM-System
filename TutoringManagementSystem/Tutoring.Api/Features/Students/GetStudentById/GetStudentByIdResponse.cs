namespace Tutoring.Api.Features.Students.GetStudentById;

public record GetStudentByIdResponse(Guid Id, string FirstName, string LastName, string Email, string? PhoneNumber, DateTimeOffset CreatedAtUtc);