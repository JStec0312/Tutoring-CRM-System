namespace Tutoring.Api.Features.Tutors.AddStudentManually;

public sealed record AddStudentManuallyResponse(
    Guid StudentId,
    Guid AgreementId);