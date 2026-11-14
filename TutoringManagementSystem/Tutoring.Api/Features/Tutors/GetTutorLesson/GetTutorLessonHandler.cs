using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Calendar.GetCalendarLessons;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.GetTutorLesson;

public sealed class GetTutorLessonHandler(TutoringDbContext db)
    : IRequestHandler<GetTutorLessonQuery, TutorLessonResponse>
{
    public async Task<TutorLessonResponse> Handle(GetTutorLessonQuery request, CancellationToken cancellationToken)
    {
        var lesson = await db.Lessons.AsNoTracking()
            .Include(lesson => lesson.Note)
            .Include(lesson => lesson.Agreement).ThenInclude(agreement => agreement.Student)
            .SingleOrDefaultAsync(lesson => lesson.Id == request.LessonId &&
                lesson.Agreement.Tutor.UserAccountId == request.AccountId, cancellationToken)
            ?? throw new LessonNotFoundException(request.AccountId.Value, request.LessonId.Value);

        var agreement = lesson.Agreement;
        var summary = GetCalendarLessonsHandler.WithEstimate(new CalendarLessonResponse(
            lesson.Id.Value, agreement.Id.Value, lesson.LessonSeriesId?.Value,
            agreement.AgreementTitle.Value, agreement.Subject.Name,
            lesson.TimeSlot.StartsAtUtc, lesson.TimeSlot.EndsAtUtc, lesson.Status.ToString(),
            agreement.StudentId.Value, agreement.Student.DisplayName.Value, agreement.TutorId.Value, null, null),
            agreement.HourlyRate);

        var note = lesson.Note;
        return new TutorLessonResponse(summary, note is null
            ? null : new TutorLessonNoteResponse(note.Topic, note.CoveredTopics, note.StudentProgress),
            lesson.CancellationReason?.Text, lesson.PlannedTopic);
    }
}
