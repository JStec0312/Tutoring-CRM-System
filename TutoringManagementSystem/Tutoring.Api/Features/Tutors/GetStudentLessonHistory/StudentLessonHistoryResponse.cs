namespace Tutoring.Api.Features.Tutors.GetStudentLessonHistory;

public sealed record StudentLessonHistoryResponse(
    Guid LessonId,
    Guid TutoringAgreementId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string Status,
    string Subject,
    string AgreementTitle,
    string? CancellationReason);
