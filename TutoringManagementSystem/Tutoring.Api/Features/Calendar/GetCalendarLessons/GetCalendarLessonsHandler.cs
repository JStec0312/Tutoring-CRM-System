using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Calendar.GetCalendarLessons;

public sealed class GetCalendarLessonsHandler(
    TutoringDbContext dbContext,
    ILogger<GetCalendarLessonsHandler> logger)
    : IRequestHandler<
        GetCalendarLessonsQuery,
        IReadOnlyCollection<CalendarLessonResponse>>
{
    public async Task<IReadOnlyCollection<CalendarLessonResponse>> Handle(
        GetCalendarLessonsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Calendar lessons query started. UserAccountId: {UserAccountId}, FromUtc: {FromUtc}, ToUtc: {ToUtc}, RequestMetadata: {RequestMetadata}",
            request.UserAccountId.Value,
            request.FromUtc,
            request.ToUtc,
            request.RequestMetadata);

        var lessons = await dbContext.Lessons
            .AsNoTracking()
            .Where(lesson =>
                (
                    lesson.Agreement.Tutor.UserAccountId
                        == request.UserAccountId ||
                    lesson.Agreement.Student.UserAccountId
                        == request.UserAccountId
                ) &&
                lesson.TimeSlot.StartsAtUtc < request.ToUtc &&
                lesson.TimeSlot.EndsAtUtc > request.FromUtc)
            .OrderBy(lesson => lesson.TimeSlot.StartsAtUtc)
            .Select(lesson => new CalendarLessonResponse(
                lesson.Id.Value,
                lesson.TutoringAgreementId.Value,
                lesson.LessonSeriesId.HasValue
                    ? lesson.LessonSeriesId.Value.Value
                    : null,
                lesson.Agreement.AgreementTitle.Value,
                lesson.Agreement.Subject.Name,
                lesson.TimeSlot.StartsAtUtc,
                lesson.TimeSlot.EndsAtUtc,
                lesson.Status.ToString(),
                lesson.Agreement.Student.Id.Value,
                lesson.Agreement.Student.DisplayName.Value,
                lesson.Agreement.Tutor.Id.Value,
                lesson.Agreement.Tutor.Account.Profile.FirstName,
                lesson.Agreement.Tutor.Account.Profile.LastName))
            .ToListAsync(cancellationToken);

        logger.LogInformation(
            "Calendar lessons query completed. UserAccountId: {UserAccountId}, LessonsCount: {LessonsCount}, FromUtc: {FromUtc}, ToUtc: {ToUtc}, RequestMetadata: {RequestMetadata}",
            request.UserAccountId.Value,
            lessons.Count,
            request.FromUtc,
            request.ToUtc,
            request.RequestMetadata);

        return lessons;
    }
}