namespace Tutoring.Api.Features.Students.GetStudentAgreements;

public sealed record StudentAgreementResponse(Guid AgreementId, string Title, string Subject, string Status,
    string? TutorFirstName, string? TutorLastName, decimal? HourlyRate, string? Currency);
