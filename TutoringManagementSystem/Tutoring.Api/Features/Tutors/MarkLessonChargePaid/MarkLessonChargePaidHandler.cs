using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.Billing;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.MarkLessonChargePaid;

public sealed class MarkLessonChargePaidHandler(
    TutoringDbContext dbContext,
    ILogger<MarkLessonChargePaidHandler> logger)
    : IRequestHandler<MarkLessonChargePaidCommand>
{
    public async Task Handle(
        MarkLessonChargePaidCommand request,
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
                "Tutor not found while marking lesson charge as paid. UserAccountId: {UserAccountId}, LessonChargeId: {LessonChargeId}, RequestMetadata: {RequestMetadata}",
                request.UserAccountId.Value,
                request.LessonChargeId,
                request.RequestMetadata);

            throw new TutorNotFoundException(
                request.UserAccountId.Value);
        }

        var chargeId = new LessonChargeId(
            request.LessonChargeId);

        var billingAccount = await dbContext.BillingAccounts
            .Include(account => account.Charges)
            .SingleOrDefaultAsync(
                account =>
                    account.Charges.Any(
                        charge => charge.Id == chargeId) &&
                    dbContext.TutoringAgreements.Any(
                        agreement =>
                            agreement.Id == account.TutoringAgreementId &&
                            agreement.TutorId == tutor.Id),
                cancellationToken);

        if (billingAccount is null)
        {
            logger.LogWarning(
                "Lesson charge not found or not owned by tutor. TutorId: {TutorId}, LessonChargeId: {LessonChargeId}, RequestMetadata: {RequestMetadata}",
                tutor.Id.Value,
                request.LessonChargeId,
                request.RequestMetadata);

            throw new LessonChargeNotFoundException(
                tutor.Id.Value,
                request.LessonChargeId);
        }

        var reference = request.Reference is null
            ? null
            : new PaymentReference(request.Reference);

        var payment = billingAccount.MarkChargeAsPaid(
            chargeId,
            request.PaidAtUtc,
            reference);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        logger.LogInformation(
            "Lesson charge marked as paid. TutorId: {TutorId}, BillingAccountId: {BillingAccountId}, LessonChargeId: {LessonChargeId}, PaymentId: {PaymentId}, PaidAtUtc: {PaidAtUtc}, RequestMetadata: {RequestMetadata}",
            tutor.Id.Value,
            billingAccount.Id.Value,
            request.LessonChargeId,
            payment.Id.Value,
            request.PaidAtUtc,
            request.RequestMetadata);
    }
}