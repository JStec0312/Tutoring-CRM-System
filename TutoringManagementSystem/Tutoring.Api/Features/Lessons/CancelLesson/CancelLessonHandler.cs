using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Lessons.Exceptions;
using Tutoring.Domain.Lessons;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Lessons.CancelLesson;

public sealed class CancelLessonHandler(
    TutoringDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<CancelLessonHandler> logger)
    : IRequestHandler<CancelLessonCommand>
{
    public async Task Handle(
        CancelLessonCommand request,
        CancellationToken cancellationToken)
    {
        var lessonId = new LessonId(request.LessonId);

        var lesson = await dbContext.Lessons
            .Include(lesson => lesson.Agreement)
                .ThenInclude(agreement => agreement.Tutor)
            .Include(lesson => lesson.Agreement)
                .ThenInclude(agreement => agreement.Student)
            .SingleOrDefaultAsync(
                lesson =>
                    lesson.Id == lessonId &&
                    (
                        lesson.Agreement.Tutor.UserAccountId
                            == request.UserAccountId ||
                        lesson.Agreement.Student.UserAccountId
                            == request.UserAccountId
                    ),
                cancellationToken);

        if (lesson is null)
        {
            logger.LogWarning(
                "Lesson not found or not accessible while cancelling. UserId: {UserId}, LessonId: {LessonId}, RequestMetadata: {RequestMetadata}",
                request.UserAccountId.Value,
                request.LessonId,
                request.RequestMetadata);

            throw new LessonNotFoundForUserException(
                request.LessonId);
        }

        var isTutor =
            lesson.Agreement.Tutor.UserAccountId
            == request.UserAccountId;

        var isStudent =
            lesson.Agreement.Student.UserAccountId
            == request.UserAccountId;

        LessonCancellationParty cancellationParty;

        if (isStudent)
        {
            if (request.CancellationParty
                == LessonCancellationParty.Tutor)
            {
                throw new CanNotCancelLessonAsATutorBeingStudentException(
                    request.LessonId);
            }

            cancellationParty = LessonCancellationParty.Student;
        }
        else if (isTutor)
        {
            cancellationParty =
                request.CancellationParty
                ?? LessonCancellationParty.Tutor;
        }
        else
        {
            throw new LessonNotFoundForUserException(
                request.LessonId);
        }

        CancellationReason? cancellationReason = null;

        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            cancellationReason =
                new CancellationReason(request.Reason);
        }


        lesson.Cancel(
            cancellationParty,
            cancellationReason,
            request.UserAccountId,
            cancelledAtUtc);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        logger.LogInformation(
            "Lesson cancelled. LessonId: {LessonId}, CancellationParty: {CancellationParty}, CancelledByUserId: {CancelledByUserId}, CancelledAtUtc: {CancelledAtUtc}, RequestMetadata: {RequestMetadata}",
            lesson.Id.Value,
            cancellationParty,
            request.UserAccountId.Value,
            cancelledAtUtc,
            request.RequestMetadata);
    }
}