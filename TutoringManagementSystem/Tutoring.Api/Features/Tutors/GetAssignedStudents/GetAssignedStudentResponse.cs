namespace Tutoring.Api.Features.Tutors.GetAssignedStudents;

public sealed record AssignedStudentResponse(
    Guid StudentId,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string Status,
    string Subject,
    decimal? HourlyRate,
    string? ContactEmail,
    string? ContactPhoneNumber,
    string? Notes);