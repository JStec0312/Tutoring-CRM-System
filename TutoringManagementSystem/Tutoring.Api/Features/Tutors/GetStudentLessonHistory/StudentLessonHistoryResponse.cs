namespace Tutoring.Api.Features.Tutors.GetStudentLessonHistory;

public sealed record StudentLessonHistoryResponse(
    Guid LessonId,
    Guid TutoringAgreementId,
    string Title,
    string Subject,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string Status);
