using MediatR;
using Microsoft.EntityFrameworkCore;
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
        var tutoringAgreementId =
            new TutoringAgreementId(request.TutoringAgreementId);

        var agreementExists = await dbContext.TutoringAgreements
            .AsNoTracking()
            .AnyAsync(
                agreement =>
                    agreement.Id == tutoringAgreementId &&
                    agreement.Tutor.UserAccountId == request.UserAccountId,
                cancellationToken);

        if (!agreementExists)
        {
            logger.LogWarning(
                "Tutoring agreement balance requested for missing or unowned agreement. TutoringAgreementId: {TutoringAgreementId}, UserAccountId: {UserAccountId}, RequestMetadata: {RequestMetadata}",
                request.TutoringAgreementId,
                request.UserAccountId.Value,
                request.RequestMetadata);

            throw new GetTutoringAgreementBalanceNotFoundException(
                request.TutoringAgreementId);
        }

        var billingAccountId = await dbContext.BillingAccounts
            .AsNoTracking()
            .Where(account => account.TutoringAgreementId == tutoringAgreementId)
            .Select(account => (Guid?)account.Id.Value)
            .SingleOrDefaultAsync(cancellationToken);

        if (billingAccountId is null)
        {
            return new GetTutoringAgreementBalanceResponse(0m, 0m, 0m);
        }

        var totals = await dbContext.LessonCharges
            .AsNoTracking()
            .Where(charge =>
                charge.BillingAccountId == new BillingAccountId(billingAccountId.Value) &&
                charge.Status == ChargeStatus.Active)
            .GroupBy(charge => charge.BillingAccountId)
            .Select(charges => new GetTutoringAgreementBalanceResponse(
                charges.Sum(charge => charge.Amount.Amount),
                charges.Sum(charge => charge.PaymentId != null
                    ? charge.Amount.Amount
                    : 0m),
                charges.Sum(charge => charge.PaymentId == null
                    ? charge.Amount.Amount
                    : 0m)))
            .SingleOrDefaultAsync(cancellationToken);

        var response = totals ?? new GetTutoringAgreementBalanceResponse(0m, 0m, 0m);

        logger.LogInformation(
            "Tutoring agreement balance retrieved. TutoringAgreementId: {TutoringAgreementId}, TotalCharged: {TotalCharged}, TotalPaid: {TotalPaid}, Balance: {Balance}, RequestMetadata: {RequestMetadata}",
            request.TutoringAgreementId,
            response.TotalCharged,
            response.TotalPaid,
            response.Balance,
            request.RequestMetadata);

        return response;
    }
}
