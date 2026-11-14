using Tutoring.Api.Features.Common.Authorization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Calendar.GetCalendarLessons;

public sealed class GetCalendarLessonsHandler(
    TutoringDbContext dbContext,
    TimeProvider timeProvider,
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

        var source = dbContext.Lessons.AsNoTracking();
        source = request.TutorOnly
            ? source.Where(lesson => lesson.Agreement.Tutor.UserAccountId == request.UserAccountId)
            : source.ForParticipant(request.UserAccountId);

        if (request.AwaitingOutcome)
        {
            var now = timeProvider.GetUtcNow();
            source = source.Where(lesson =>
                lesson.Status == LessonStatus.Scheduled && lesson.TimeSlot.EndsAtUtc <= now);
        }

        if (request.AgreementId is { } agreementId)
        {
            var id = new TutoringAgreementId(agreementId);
            source = source.Where(lesson => lesson.TutoringAgreementId == id);
        }

        var rows = await source
            .Where(lesson =>
                lesson.TimeSlot.StartsAtUtc < request.ToUtc &&
                lesson.TimeSlot.EndsAtUtc > request.FromUtc)
            .OrderBy(lesson => lesson.TimeSlot.StartsAtUtc)
            .Select(lesson => new
            {
                Lesson = new CalendarLessonResponse(
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
                lesson.Agreement.Tutor.Account.Profile.LastName),
                Rate = request.TutorOnly ? lesson.Agreement.HourlyRate : null
            })
            .ToListAsync(cancellationToken);

        var lessons = rows.Select(row => WithEstimate(row.Lesson, row.Rate)).ToArray();

        logger.LogInformation(
            "Calendar lessons query completed. UserAccountId: {UserAccountId}, LessonsCount: {LessonsCount}, FromUtc: {FromUtc}, ToUtc: {ToUtc}, RequestMetadata: {RequestMetadata}",
            request.UserAccountId.Value,
            lessons.Length,
            request.FromUtc,
            request.ToUtc,
            request.RequestMetadata);

        return lessons;
    }

    internal static CalendarLessonResponse WithEstimate(CalendarLessonResponse lesson, HourlyRate? rate)
    {
        if (rate is null || lesson.Status != nameof(LessonStatus.Scheduled))
            return lesson;

        var estimate = rate.CalculateCost(lesson.EndsAtUtc - lesson.StartsAtUtc);
        return lesson with { EstimatedAmount = estimate.Amount, Currency = estimate.Currency.Code };
    }
}
