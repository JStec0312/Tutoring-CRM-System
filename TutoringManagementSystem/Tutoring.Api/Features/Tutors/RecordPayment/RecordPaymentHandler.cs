using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.Billing;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.RecordPayment;

public sealed class RecordPaymentHandler(
    TutoringDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<RecordPaymentHandler> logger)
    : IRequestHandler<RecordPaymentCommand, RecordPaymentResponse>
{
    public async Task<RecordPaymentResponse> Handle(
        RecordPaymentCommand request,
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
                "Tutor not found while recording payment. UserId: {UserId}, TutoringAgreementId: {TutoringAgreementId}, RequestMetadata: {RequestMetadata}",
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
                "Tutoring agreement not found or not owned by tutor while recording payment. TutorId: {TutorId}, TutoringAgreementId: {TutoringAgreementId}, RequestMetadata: {RequestMetadata}",
                tutor.Id.Value,
                request.TutoringAgreementId,
                request.RequestMetadata);

            throw new TutoringAgreementNotFoundByIdException(
                tutor.Id.Value,
                request.TutoringAgreementId);
        }

        var billingAccount = await dbContext.BillingAccounts
            .SingleOrDefaultAsync(
                account => account.TutoringAgreementId == agreement.Id,
                cancellationToken);

        if (billingAccount is null)
        {
            billingAccount = new BillingAccount(
                agreement.Id,
                timeProvider.GetUtcNow());

            dbContext.BillingAccounts.Add(billingAccount);
        }

        var amount = new Money(
            request.Amount,
            new Currency("PLN"));

        var reference = request.Reference is null
            ? null
            : new PaymentReference(request.Reference);

        var payment = billingAccount.RecordPayment(
            amount,
            request.PaidAtUtc,
            reference);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Payment recorded. PaymentId: {PaymentId}, BillingAccountId: {BillingAccountId}, TutorId: {TutorId}, TutoringAgreementId: {TutoringAgreementId}, Amount: {Amount}, PaidAtUtc: {PaidAtUtc}, RequestMetadata: {RequestMetadata}",
            payment.Id.Value,
            billingAccount.Id.Value,
            tutor.Id.Value,
            agreement.Id.Value,
            payment.Amount.Amount,
            payment.PaidAtUtc,
            request.RequestMetadata);

        return new RecordPaymentResponse(
            payment.Id.Value,
            payment.Amount.Amount,
            payment.Amount.Currency.Code,
            payment.PaidAtUtc,
            payment.Reference?.Value);
    }
}
