using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.Students;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.GetStudentLessonHistory;

public sealed class GetStudentLessonHistoryHandler(
    TutoringDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<GetStudentLessonHistoryHandler> logger)
    : IRequestHandler<
        GetStudentLessonHistoryQuery,
        IReadOnlyCollection<StudentLessonHistoryResponse>>
{
    public async Task<IReadOnlyCollection<StudentLessonHistoryResponse>> Handle(
        GetStudentLessonHistoryQuery request,
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
                "Tutor not found while retrieving student lesson history. UserAccountId: {UserAccountId}, StudentId: {StudentId}, RequestMetadata: {RequestMetadata}",
                request.UserAccountId.Value,
                request.StudentId,
                request.RequestMetadata);

            throw new TutorNotFoundException(request.UserAccountId.Value);
        }

        var studentId = new StudentId(request.StudentId);
        var hasAgreement = await dbContext.TutoringAgreements
            .AsNoTracking()
            .AnyAsync(
                agreement =>
                    agreement.TutorId == tutor.Id &&
                    agreement.StudentId == studentId,
                cancellationToken);

        if (!hasAgreement)
        {
            logger.LogWarning(
                "Tutoring agreement not found while retrieving student lesson history. TutorId: {TutorId}, StudentId: {StudentId}, RequestMetadata: {RequestMetadata}",
                tutor.Id.Value,
                request.StudentId,
                request.RequestMetadata);

            throw new TutoringAgreementNotFoundException(
                tutor.Id.Value,
                request.StudentId);
        }

        var nowUtc = timeProvider.GetUtcNow();
        var lessons = await dbContext.Lessons
            .AsNoTracking()
            .Where(lesson =>
                lesson.Agreement.TutorId == tutor.Id &&
                lesson.Agreement.StudentId == studentId &&
                lesson.TimeSlot.StartsAtUtc < nowUtc)
            .OrderByDescending(lesson => lesson.TimeSlot.StartsAtUtc)
            .Select(lesson => new StudentLessonHistoryResponse(
                lesson.Id.Value,
                lesson.TutoringAgreementId.Value,
                lesson.Agreement.AgreementTitle.Value,
                lesson.Agreement.Subject.Name,
                lesson.TimeSlot.StartsAtUtc,
                lesson.TimeSlot.EndsAtUtc,
                lesson.Status.ToString()))
            .ToListAsync(cancellationToken);

        logger.LogInformation(
            "Student lesson history retrieved. TutorId: {TutorId}, StudentId: {StudentId}, LessonsCount: {LessonsCount}, RequestMetadata: {RequestMetadata}",
            tutor.Id.Value,
            request.StudentId,
            lessons.Count,
            request.RequestMetadata);

        return lessons;
    }
}
