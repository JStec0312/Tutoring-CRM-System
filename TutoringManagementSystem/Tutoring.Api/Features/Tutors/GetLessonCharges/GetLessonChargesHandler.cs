using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Common.Authorization;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.GetLessonCharges;

public sealed class GetLessonChargesHandler(TutoringDbContext db)
    : IRequestHandler<GetLessonChargesQuery, IReadOnlyList<LessonChargeResponse>>
{
    public async Task<IReadOnlyList<LessonChargeResponse>> Handle(
        GetLessonChargesQuery request, CancellationToken cancellationToken)
    {
        var agreements = db.TutoringAgreements.AsNoTracking().ForTutor(request.AccountId);
        if (request.AgreementId is { } agreementId)
        {
            var id = new TutoringAgreementId(agreementId);
            agreements = agreements.Where(agreement => agreement.Id == id);
        }

        var lessons = db.Lessons.AsNoTracking();
        if (request.LessonId is { } lessonId)
        {
            var id = new LessonId(lessonId);
            lessons = lessons.Where(lesson => lesson.Id == id);
        }

        return await (
            from charge in db.LessonCharges.AsNoTracking()
            join account in db.BillingAccounts.AsNoTracking() on charge.BillingAccountId equals account.Id
            join lesson in lessons on charge.LessonId equals lesson.Id
            join agreement in agreements on account.TutoringAgreementId equals agreement.Id
            where charge.Status == ChargeStatus.Active && lesson.TutoringAgreementId == agreement.Id
            orderby lesson.TimeSlot.StartsAtUtc descending, charge.Id
            select new LessonChargeResponse(
                charge.Id.Value, agreement.Id.Value, lesson.Id.Value,
                agreement.Student.DisplayName.Value, agreement.Subject.Name,
                lesson.TimeSlot.StartsAtUtc, lesson.TimeSlot.EndsAtUtc,
                charge.Amount.Amount, charge.Amount.Currency.Code,
                charge.Payment == null ? null : charge.Payment.PaidAtUtc)
        ).ToListAsync(cancellationToken);
    }
}
