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
        var unpaidByAgreement =
            from charge in dbContext.LessonCharges.AsNoTracking()
            join account in dbContext.BillingAccounts.AsNoTracking()
                on charge.BillingAccountId equals account.Id
            where charge.Status == ChargeStatus.Active
                  && charge.PaymentId == null
            group charge by account.TutoringAgreementId
            into unpaidCharges
            select new
            {
                TutoringAgreementId = unpaidCharges.Key,
                TotalUnpaidAmount = unpaidCharges.Sum(charge => charge.Amount.Amount),
                UnpaidChargeCount = unpaidCharges.Count()
            };

        var agreements = await (
            from unpaid in unpaidByAgreement
            join agreement in dbContext.TutoringAgreements.AsNoTracking()
                on unpaid.TutoringAgreementId equals agreement.Id
            where agreement.Tutor.UserAccountId == request.UserAccountId
            orderby unpaid.TotalUnpaidAmount descending
            select new OverdueTutoringAgreementResponse(
                agreement.Id.Value,
                agreement.Student.Id.Value,
                agreement.Student.DisplayName.Value,
                agreement.Subject.Name,
                agreement.AgreementTitle.Value,
                unpaid.TotalUnpaidAmount,
                unpaid.UnpaidChargeCount))
            .ToListAsync(cancellationToken);

        logger.LogInformation(
            "Overdue tutoring agreements retrieved. UserAccountId: {UserAccountId}, AgreementCount: {AgreementCount}, RequestMetadata: {RequestMetadata}",
            request.UserAccountId.Value,
            agreements.Count,
            request.RequestMetadata);

        return agreements;
    }
}