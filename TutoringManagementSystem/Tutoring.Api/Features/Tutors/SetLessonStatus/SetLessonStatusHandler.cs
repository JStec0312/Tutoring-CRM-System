using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Lessons;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.SetLessonStatus;

public sealed class SetLessonStatusHandler(
    TutoringDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<SetLessonStatusHandler> logger)
    : IRequestHandler<SetLessonStatusCommand>
{
    public async Task Handle(
        SetLessonStatusCommand request,
        CancellationToken cancellationToken)
    {
        var tutor = await dbContext.Tutors
            .AsNoTracking()
            .SingleOrDefaultAsync(
                tutor => tutor.UserAccountId == request.UserAccountId,
                cancellationToken);

        if (tutor is null)
        {
            throw new TutorNotFoundException(
                request.UserAccountId.Value);
        }

        var lessonId = new LessonId(request.LessonId);

        var lesson = await dbContext.Lessons
            .Include(lesson => lesson.Agreement)
                .ThenInclude(agreement => agreement.BillingAccount)
            .SingleOrDefaultAsync(
                lesson =>
                    lesson.Id == lessonId &&
                    lesson.Agreement.TutorId == tutor.Id,
                cancellationToken);
        if (lesson is null)
        {
            logger.LogWarning(
                "Lesson not found or not owned by tutor while setting lesson status. TutorId: {TutorId}, LessonId: {LessonId}, RequestMetadata: {RequestMetadata}",
                tutor.Id.Value,
                request.LessonId,
                request.RequestMetadata);

            throw new LessonNotFoundException(
                tutor.Id.Value,
                request.LessonId);
        }

        var nowUtc = timeProvider.GetUtcNow();

        switch (request.Status)
        {
            case LessonStatus.Completed:
                lesson.Complete(nowUtc);
                // Implementation of US - 016 charging for lesson 
                // lazy creation of billing account and adding lesson charge
                var lessonTutoringAgreement = lesson.Agreement;
                if (lessonTutoringAgreement.HasHourlyRate)
                {
                    var lessonbillingAccount = lessonTutoringAgreement.BillingAccount;
                    if(lessonbillingAccount is null)
                    {

                        BillingAccount billingAccount = new BillingAccount(lessonTutoringAgreement.Id, nowUtc);
                        lessonTutoringAgreement.AddBillingAccount(billingAccount);
                        lessonbillingAccount = billingAccount;
                    }
                    lessonbillingAccount.AddLessonCharge(lesson, lessonTutoringAgreement.HourlyRate!, nowUtc);
                }

                break;

            case LessonStatus.Missed:
                lesson.MarkAsMissed(nowUtc);
                break;

            default:
                throw new ArgumentException(
                    "Lesson status can only be set to Completed or Missed.",
                    nameof(request.Status));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Lesson finalized. LessonId: {LessonId}, TutorId: {TutorId}, Status: {Status}, RequestMetadata: {RequestMetadata}",
            lesson.Id.Value,
            tutor.Id.Value,
            lesson.Status,
            request.RequestMetadata);
    }
}