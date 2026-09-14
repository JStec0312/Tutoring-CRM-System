namespace Tutoring.Api.Features.Tutors.GetAssignedStudents;

public sealed record AssignedStudentResponse(
    Guid StudentId,
    string UserName,
    string? FirstName,
    string? LastName,
    string Email,
    string? PhoneNumber,
    string Status);