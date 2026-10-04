namespace Tutoring.Api.Features.Tutors.GetOverdueTutoringAgreements;

public sealed record OverdueTutoringAgreementResponse(
    Guid TutoringAgreementId,
    Guid StudentId,
    string StudentDisplayName,
    string Subject,
    string AgreementTitle,
    decimal OutstandingAmount,
    int UnpaidLessonCount);
