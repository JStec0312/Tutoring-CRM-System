namespace Tutoring.Api.Features.Tutors.ScheduleLesson;

public sealed record ScheduleLessonRequest(
    Guid TutoringAgreementId,
    DateTimeOffset StartsAt,
    int DurationMinutes);
