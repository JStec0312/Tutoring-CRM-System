using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.ScheduleLesson;

public sealed class ScheduleLessonHandler(
    TutoringDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<ScheduleLessonHandler> logger)
    : IRequestHandler<ScheduleLessonCommand, ScheduleLessonResponse>
{
    public async Task<ScheduleLessonResponse> Handle(
        ScheduleLessonCommand request,
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
                "Tutor not found while scheduling lesson. UserId: {UserId}, TutoringAgreementId: {TutoringAgreementId}, RequestMetadata: {RequestMetadata}",
                request.UserAccountId.Value,
                request.TutoringAgreementId,
                request.RequestMetadata);

            throw new TutorNotFoundException(request.UserAccountId.Value);
        }

        var agreement = await dbContext.TutoringAgreements
            .AsNoTracking()
            .SingleOrDefaultAsync(
                agreement =>
                    agreement.Id == new TutoringAgreementId(request.TutoringAgreementId) &&
                    agreement.TutorId == tutor.Id,
                cancellationToken);

        if (agreement is null)
        {
            logger.LogWarning(
                "Tutoring agreement not found or not owned by tutor while scheduling lesson. TutorId: {TutorId}, TutoringAgreementId: {TutoringAgreementId}, RequestMetadata: {RequestMetadata}",
                tutor.Id.Value,
                request.TutoringAgreementId,
                request.RequestMetadata);

            throw new TutoringAgreementNotFoundByIdException(
                tutor.Id.Value,
                request.TutoringAgreementId);
        }

        if (agreement.Status != AgreementStatus.Active)
        {
            throw new TutoringAgreementNotActiveException(
                agreement.Id.Value);
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

        var endsAtUtc = startsAtUtc.AddMinutes(request.DurationMinutes);

        var hasTimeConflict = await dbContext.Lessons
            .AsNoTracking()
            .AnyAsync(
                lesson =>
                    lesson.Agreement.TutorId == tutor.Id &&
                    lesson.Status == LessonStatus.Scheduled &&
                    lesson.TimeSlot.StartsAtUtc < endsAtUtc &&
                    lesson.TimeSlot.EndsAtUtc > startsAtUtc,
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

        var lesson = new Lesson(
            agreement.Id,
            timeSlot,
            now);

        dbContext.Lessons.Add(lesson);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Lesson scheduled. LessonId: {LessonId}, TutorId: {TutorId}, TutoringAgreementId: {TutoringAgreementId}, StartsAtUtc: {StartsAtUtc}, EndsAtUtc: {EndsAtUtc}, RequestMetadata: {RequestMetadata}",
            lesson.Id.Value,
            tutor.Id.Value,
            agreement.Id.Value,
            startsAtUtc,
            endsAtUtc,
            request.RequestMetadata);

        return new ScheduleLessonResponse(lesson.Id.Value);
    }
}
