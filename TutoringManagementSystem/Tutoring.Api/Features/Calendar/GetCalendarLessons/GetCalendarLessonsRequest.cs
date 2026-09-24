namespace Tutoring.Api.Features.Calendar.GetCalendarLessons;
public sealed record GetCalendarLessonsRequest(
    DateTimeOffset From,
    DateTimeOffset To);