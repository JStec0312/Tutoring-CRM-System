using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.Lessons;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.RescheduleLesson;

public sealed class RescheduleLessonHandler(
    TutoringDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<RescheduleLessonHandler> logger)
    : IRequestHandler<RescheduleLessonCommand>
{
    public async Task Handle(
        RescheduleLessonCommand request,
        CancellationToken cancellationToken)
    {
        var tutor = await dbContext.Tutors
            .AsNoTracking()
            .SingleOrDefaultAsync(
                tutor => tutor.UserAccountId == request.UserAccountId,
                cancellationToken);

        if (tutor is null)
        {
            logger.LogWarning(
                "Tutor not found while rescheduling lesson. UserId: {UserId}, LessonId: {LessonId}, RequestMetadata: {RequestMetadata}",
                request.UserAccountId.Value,
                request.LessonId,
                request.RequestMetadata);

            throw new TutorNotFoundException(
                request.UserAccountId.Value);
        }

        var lessonId = new LessonId(request.LessonId);

        var lesson = await dbContext.Lessons
            .SingleOrDefaultAsync(
                lesson =>
                    lesson.Id == lessonId &&
                    lesson.Agreement.TutorId == tutor.Id,
                cancellationToken);

        if (lesson is null)
        {
            logger.LogWarning(
                "Lesson not found or not owned by tutor while rescheduling. TutorId: {TutorId}, LessonId: {LessonId}, RequestMetadata: {RequestMetadata}",
                tutor.Id.Value,
                request.LessonId,
                request.RequestMetadata);

            throw new LessonNotFoundException(
                tutor.Id.Value,
                request.LessonId);
        }

        if (request.DurationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.DurationMinutes),
                request.DurationMinutes,
                "Lesson duration must be greater than zero.");
        }

        var startsAtUtc = request.StartsAt.ToUniversalTime();
        var now = timeProvider.GetUtcNow();

        if (startsAtUtc <= now)
        {
            throw new ArgumentException(
                "Lesson start time must be in the future.",
                nameof(request.StartsAt));
        }

        var endsAtUtc = startsAtUtc.AddMinutes(
            request.DurationMinutes);

        var hasTimeConflict = await dbContext.Lessons
            .AsNoTracking()
            .AnyAsync(
                existingLesson =>
                    existingLesson.Id != lesson.Id &&
                    existingLesson.Agreement.TutorId == tutor.Id &&
                    existingLesson.Status == LessonStatus.Scheduled &&
                    existingLesson.TimeSlot.StartsAtUtc < endsAtUtc &&
                    existingLesson.TimeSlot.EndsAtUtc > startsAtUtc,
                cancellationToken);

        if (hasTimeConflict)
        {
            throw new LessonTimeConflictException(
                startsAtUtc,
                endsAtUtc);
        }

        var timeSlot = new TimeSlot(
            startsAtUtc,
            endsAtUtc);

        lesson.Reschedule(timeSlot);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Lesson rescheduled. LessonId: {LessonId}, TutorId: {TutorId}, StartsAtUtc: {StartsAtUtc}, EndsAtUtc: {EndsAtUtc}, RequestMetadata: {RequestMetadata}",
            lesson.Id.Value,
            tutor.Id.Value,
            startsAtUtc,
            endsAtUtc,
            request.RequestMetadata);
    }
}