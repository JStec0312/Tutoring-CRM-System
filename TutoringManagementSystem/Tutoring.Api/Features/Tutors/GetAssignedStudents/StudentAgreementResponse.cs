namespace Tutoring.Api.Features.Tutors.GetAssignedStudents;

public sealed record StudentAgreementResponse(
    Guid TutoringAgreementId,
    string Subject,
    decimal? HourlyRate,
    string? Notes,
    string Title,
    string Status,
    string? Currency);
