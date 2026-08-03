using Tutoring.Domain.Students;

namespace Tutoring.Api.Features.Students.GetStudentById;

public record GetStudentByIdResponse(
    Guid Id,
    Guid UserAccountId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    StudentStatus Status,
    DateTimeOffset CreatedAtUtc);
