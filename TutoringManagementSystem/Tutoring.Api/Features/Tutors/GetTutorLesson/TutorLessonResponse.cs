using Tutoring.Api.Features.Calendar.GetCalendarLessons;

namespace Tutoring.Api.Features.Tutors.GetTutorLesson;

public sealed record TutorLessonResponse(
    CalendarLessonResponse Lesson, TutorLessonNoteResponse? Note, string? CancellationReason, string? PlannedTopic);
