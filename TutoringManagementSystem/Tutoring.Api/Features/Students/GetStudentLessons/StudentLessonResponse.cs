namespace Tutoring.Api.Features.Students.GetStudentLessons;

public sealed record StudentLessonResponse(Guid LessonId, Guid TutoringAgreementId, string Subject,
    DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, string Status, string? PlannedTopic,
    string? CancellationReason);
