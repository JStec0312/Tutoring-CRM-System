using Tutoring.Api.Features.Common.Authorization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Billing;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.GetOverdueTutoringAgreements;

public sealed class GetOverdueTutoringAgreementsHandler(
    TutoringDbContext dbContext,
    ILogger<GetOverdueTutoringAgreementsHandler> logger)
    : IRequestHandler<
        GetOverdueTutoringAgreementsQuery,
        IReadOnlyCollection<OverdueTutoringAgreementResponse>>
{
    public async Task<IReadOnlyCollection<OverdueTutoringAgreementResponse>> Handle(
        GetOverdueTutoringAgreementsQuery request,
        CancellationToken cancellationToken)
    {
        var ownedAgreements = dbContext.TutoringAgreements.AsNoTracking().ForTutor(request.UserAccountId);
        var ownedCharges =
            from charge in dbContext.LessonCharges.AsNoTracking()
            join account in dbContext.BillingAccounts.AsNoTracking()
                on charge.BillingAccountId equals account.Id
            where charge.Status == ChargeStatus.Active
                  && charge.PaymentId == null
                  && ownedAgreements.Any(agreement => agreement.Id == account.TutoringAgreementId)
            select new { account.TutoringAgreementId, charge.Amount };

        var unpaidByCurrency = await (
            from charge in ownedCharges
            group charge by new { charge.TutoringAgreementId, charge.Amount.Currency } into unpaidCharges
            select new
            {
                unpaidCharges.Key.TutoringAgreementId,
                unpaidCharges.Key.Currency,
                Amount = unpaidCharges.Sum(charge => charge.Amount.Amount),
                UnpaidChargeCount = unpaidCharges.Count()
            }).ToListAsync(cancellationToken);

        var agreementIds = unpaidByCurrency.Select(item => item.TutoringAgreementId).Distinct().ToArray();
        var agreementDetails = await ownedAgreements.Where(agreement => agreementIds.Contains(agreement.Id))
            .Select(agreement => new
            {
                agreement.Id,
                agreement.StudentId,
                Name = agreement.Student.DisplayName.Value,
                Subject = agreement.Subject.Name,
                Title = agreement.AgreementTitle.Value
            })
            .ToListAsync(cancellationToken);

        var agreements = agreementDetails.Join(
            unpaidByCurrency.GroupBy(item => item.TutoringAgreementId),
            agreement => agreement.Id,
            group => group.Key,
            (agreement, group) => new OverdueTutoringAgreementResponse(
                agreement.Id.Value,
                agreement.StudentId.Value,
                agreement.Name,
                agreement.Subject,
                agreement.Title,
                group.Sum(item => item.Amount),
                group.Sum(item => item.UnpaidChargeCount),
                group.OrderBy(item => item.Currency.Code, StringComparer.Ordinal)
                    .Select(item => new UnpaidAmountByCurrency(
                        item.Currency.Code,
                        item.Amount,
                        item.UnpaidChargeCount))
                    .ToArray()))
            .OrderByDescending(item => item.OutstandingAmount)
            .ToArray();

        logger.LogInformation(
            "Overdue tutoring agreements retrieved. UserAccountId: {UserAccountId}, AgreementCount: {AgreementCount}, RequestMetadata: {RequestMetadata}",
            request.UserAccountId.Value,
            agreements.Length,
            request.RequestMetadata);

        return agreements;
    }
}
