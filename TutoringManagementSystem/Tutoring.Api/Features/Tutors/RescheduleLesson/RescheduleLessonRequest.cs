namespace Tutoring.Api.Features.Tutors.RescheduleLesson;

public sealed record RescheduleLessonRequest(
    DateTimeOffset StartsAt,
    int DurationMinutes);