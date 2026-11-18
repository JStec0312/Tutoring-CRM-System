using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Students.Exceptions;
using Tutoring.Domain.Billing;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Students.GetStudentCharges;

public sealed class GetStudentChargesHandler(TutoringDbContext db)
    : IRequestHandler<GetStudentChargesQuery, IReadOnlyList<StudentChargeResponse>>
{
    public async Task<IReadOnlyList<StudentChargeResponse>> Handle(
        GetStudentChargesQuery request, CancellationToken cancellationToken)
    {
        var agreementId = new TutoringAgreementId(request.AgreementId);
        if (!await db.TutoringAgreements.AsNoTracking().AnyAsync(agreement =>
            agreement.Id == agreementId && agreement.Student.UserAccountId == request.AccountId, cancellationToken))
        {
            throw new StudentAgreementNotFoundException();
        }

        return await (
            from charge in db.LessonCharges.AsNoTracking()
            join account in db.BillingAccounts.AsNoTracking() on charge.BillingAccountId equals account.Id
            join lesson in db.Lessons.AsNoTracking() on charge.LessonId equals lesson.Id
            where account.TutoringAgreementId == agreementId && lesson.TutoringAgreementId == agreementId
                && charge.Status == ChargeStatus.Active
            orderby lesson.TimeSlot.StartsAtUtc descending, charge.Id
            select new StudentChargeResponse(charge.Id.Value, lesson.Id.Value,
                lesson.TimeSlot.StartsAtUtc, charge.Amount.Amount, charge.Amount.Currency.Code,
                charge.Payment == null ? null : charge.Payment.PaidAtUtc)
        ).ToListAsync(cancellationToken);
    }
}
