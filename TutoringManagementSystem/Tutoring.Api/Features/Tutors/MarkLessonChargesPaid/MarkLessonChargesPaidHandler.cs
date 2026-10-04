using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.Billing;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.MarkLessonChargesPaid;

public sealed class MarkLessonChargesPaidHandler(
    TutoringDbContext dbContext,
    ILogger<MarkLessonChargesPaidHandler> logger) : IRequestHandler<MarkLessonChargesPaidCommand>
{
    public async Task Handle(MarkLessonChargesPaidCommand request, CancellationToken cancellationToken)
    {
        if (request.LessonChargeIds.Count == 0)
        {
            throw new ArgumentException("At least one lesson charge is required.", nameof(request.LessonChargeIds));
        }

        var tutor = await dbContext.Tutors.AsNoTracking()
            .SingleOrDefaultAsync(tutor => tutor.UserAccountId == request.UserAccountId, cancellationToken);
        if (tutor is null)
        {
            throw new TutorNotFoundException(request.UserAccountId.Value);
        }

        var chargeIds = request.LessonChargeIds.Select(id => new LessonChargeId(id)).ToArray();
        if (chargeIds.Distinct().Count() != chargeIds.Length)
        {
            throw new ArgumentException("Lesson charge IDs must be unique.", nameof(request.LessonChargeIds));
        }

        var accounts = await dbContext.BillingAccounts
            .Include(account => account.Charges)
            .Where(account =>
                account.Charges.Any(charge => chargeIds.Contains(charge.Id)) &&
                dbContext.TutoringAgreements.Any(agreement =>
                    agreement.Id == account.TutoringAgreementId && agreement.TutorId == tutor.Id))
            .ToListAsync(cancellationToken);

        var selectedCharges = accounts.SelectMany(account => account.Charges)
            .Where(charge => chargeIds.Contains(charge.Id))
            .ToArray();

       if (selectedCharges.Length != chargeIds.Length)
        {
            logger.LogWarning(
                "One or more lesson charges were not found or are not owned by tutor. TutorId: {TutorId}, ChargeCount: {ChargeCount}, RequestMetadata: {RequestMetadata}",
                tutor.Id.Value,
                chargeIds.Length,
                request.RequestMetadata);

            throw new LessonChargeNotFoundException(
                tutor.Id.Value,
                request.LessonChargeIds.First());
        }

        if (accounts.Count != 1)
        {
            logger.LogWarning(
                "Lesson charges belong to different billing accounts. TutorId: {TutorId}, ChargeCount: {ChargeCount}, BillingAccountCount: {BillingAccountCount}, RequestMetadata: {RequestMetadata}",
                tutor.Id.Value,
                chargeIds.Length,
                accounts.Count,
                request.RequestMetadata);

            throw new LessonChargesFromDifferentBillingAccountsException();
        }
        accounts[0].MarkChargesAsPaid(
            chargeIds,
            request.PaidAtUtc,
            request.Reference is null ? null : new PaymentReference(request.Reference));

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Lesson charges marked as paid. TutorId: {TutorId}, ChargeCount: {ChargeCount}, RequestMetadata: {RequestMetadata}",
            tutor.Id.Value,
            chargeIds.Length,
            request.RequestMetadata);
    }
}
