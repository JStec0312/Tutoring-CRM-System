using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.Billing;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.GetTutoringAgreementBalance;

public sealed class GetTutoringAgreementBalanceHandler(
    TutoringDbContext dbContext,
    ILogger<GetTutoringAgreementBalanceHandler> logger)
    : IRequestHandler<
        GetTutoringAgreementBalanceQuery,
        GetTutoringAgreementBalanceResponse>
{
    public async Task<GetTutoringAgreementBalanceResponse> Handle(
        GetTutoringAgreementBalanceQuery request,
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
                "Tutor not found while retrieving tutoring agreement balance. UserAccountId: {UserAccountId}, TutoringAgreementId: {TutoringAgreementId}, RequestMetadata: {RequestMetadata}",
                request.UserAccountId.Value,
                request.TutoringAgreementId,
                request.RequestMetadata);

            throw new TutorNotFoundException(request.UserAccountId.Value);
        }

        var agreementId = new TutoringAgreementId(request.TutoringAgreementId);
        var agreementExists = await dbContext.TutoringAgreements
            .AsNoTracking()
            .AnyAsync(
                agreement =>
                    agreement.Id == agreementId &&
                    agreement.TutorId == tutor.Id,
                cancellationToken);

        if (!agreementExists)
        {
            logger.LogWarning(
                "Tutoring agreement not found or not owned by tutor while retrieving balance. TutorId: {TutorId}, TutoringAgreementId: {TutoringAgreementId}, RequestMetadata: {RequestMetadata}",
                tutor.Id.Value,
                request.TutoringAgreementId,
                request.RequestMetadata);

            throw new TutoringAgreementNotFoundByIdException(
                tutor.Id.Value,
                request.TutoringAgreementId);
        }

        var aggregate = await dbContext.BillingAccounts
            .AsNoTracking()
            .Where(account => account.TutoringAgreementId == agreementId)
            .Select(account => new
            {
                TotalCharged = account.Charges
                    .Where(charge => charge.Status == ChargeStatus.Active)
                    .Sum(charge => (decimal?)charge.Amount.Amount) ?? 0m,
                TotalPaid = account.Payments
                    .Sum(payment => (decimal?)payment.Amount.Amount) ?? 0m
            })
            .SingleOrDefaultAsync(cancellationToken);

        var totalCharged = aggregate?.TotalCharged ?? 0m;
        var totalPaid = aggregate?.TotalPaid ?? 0m;
        var balance = totalCharged - totalPaid;

        logger.LogInformation(
            "Tutoring agreement balance retrieved. TutorId: {TutorId}, TutoringAgreementId: {TutoringAgreementId}, TotalCharged: {TotalCharged}, TotalPaid: {TotalPaid}, Balance: {Balance}, RequestMetadata: {RequestMetadata}",
            tutor.Id.Value,
            request.TutoringAgreementId,
            totalCharged,
            totalPaid,
            balance,
            request.RequestMetadata);

        return new GetTutoringAgreementBalanceResponse(
            request.TutoringAgreementId,
            totalCharged,
            totalPaid,
            balance,
            "PLN");
    }
}
