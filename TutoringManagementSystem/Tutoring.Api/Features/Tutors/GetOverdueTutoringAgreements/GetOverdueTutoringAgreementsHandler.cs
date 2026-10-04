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
        var agreements = await (
            from charge in dbContext.LessonCharges.AsNoTracking()
            join account in dbContext.BillingAccounts.AsNoTracking()
                on charge.BillingAccountId equals account.Id
            join agreement in dbContext.TutoringAgreements.AsNoTracking()
                on account.TutoringAgreementId equals agreement.Id
            where charge.Status == ChargeStatus.Active &&
                  charge.PaymentId == null &&
                  agreement.Tutor.UserAccountId == request.UserAccountId
            group charge by new
            {
                TutoringAgreementId = agreement.Id.Value,
                StudentId = agreement.Student.Id.Value,
                StudentDisplayName = agreement.Student.DisplayName.Value,
                Subject = agreement.Subject.Name,
                AgreementTitle = agreement.AgreementTitle.Value
            }
            into unpaidCharges
            orderby unpaidCharges.Sum(charge => charge.Amount.Amount) descending
            select new OverdueTutoringAgreementResponse(
                unpaidCharges.Key.TutoringAgreementId,
                unpaidCharges.Key.StudentId,
                unpaidCharges.Key.StudentDisplayName,
                unpaidCharges.Key.Subject,
                unpaidCharges.Key.AgreementTitle,
                unpaidCharges.Sum(charge => charge.Amount.Amount),
                unpaidCharges.Count()))
            .ToListAsync(cancellationToken);

        logger.LogInformation(
            "Overdue tutoring agreements retrieved. UserAccountId: {UserAccountId}, AgreementCount: {AgreementCount}, RequestMetadata: {RequestMetadata}",
            request.UserAccountId.Value,
            agreements.Count,
            request.RequestMetadata);

        return agreements;
    }
}
