namespace Tutoring.Api.Features.Students.GetStudentLesson;

public sealed record StudentLessonDetailsResponse(Guid LessonId, Guid TutoringAgreementId, string Subject,
    DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, string Status, string? PlannedTopic,
    string? CancellationReason, string? TutorFirstName, string? TutorLastName,
    decimal? EstimatedAmount, string? Currency);
