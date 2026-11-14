namespace Tutoring.Api.Features.Calendar.GetCalendarLessons;
public sealed record GetCalendarLessonsRequest(
    DateTimeOffset From,
    DateTimeOffset To,
    bool TutorOnly = false,
    bool AwaitingOutcome = false,
    Guid? AgreementId = null);
