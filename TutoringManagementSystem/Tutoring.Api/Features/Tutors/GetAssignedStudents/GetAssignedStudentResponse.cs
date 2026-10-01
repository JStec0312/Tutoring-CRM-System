namespace Tutoring.Api.Features.Tutors.GetAssignedStudents;

public sealed record AssignedStudentResponse(
    Guid StudentId,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string Status,
    IReadOnlyCollection<StudentAgreementResponse> Agreements);

public sealed record StudentAgreementResponse(
    Guid TutoringAgreementId,
    string Subject,
    decimal? HourlyRate,
    string? Notes);