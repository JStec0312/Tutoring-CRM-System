using Tutoring.Api.Features.Calendar.GetCalendarLessons;

namespace Tutoring.Api.Features.Tutors.GetTutoringAgreementOverview;

public sealed record TutoringAgreementOverviewResponse(
    Guid StudentId,
    string StudentDisplayName,
    string Subject,
    string Status,
    decimal? HourlyRate,
    string? Currency,
    string? Notes,
    CalendarLessonResponse? NextLesson,
    int MaterialCount);
