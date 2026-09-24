namespace Tutoring.Api.Features.Calendar.GetCalendarLessons;

public sealed record CalendarLessonResponse(
    Guid LessonId,
    Guid TutoringAgreementId,
    Guid? LessonSeriesId,
    string Title,
    string Subject,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string Status,
    Guid StudentId,
    string StudentDisplayName,
    Guid TutorId,
    string? TutorFirstName,
    string? TutorLastName);