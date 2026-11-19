using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Lessons.GetMyLessons;

public sealed class GetMyLessonsHandler(TutoringDbContext db, TimeProvider clock, ILogger<GetMyLessonsHandler> logger)
    : IRequestHandler<GetMyLessonsQuery, LessonPageResponse>
{
    public async Task<LessonPageResponse> Handle(GetMyLessonsQuery request, CancellationToken cancellationToken)
    {
        var lessons = db.Lessons.AsNoTracking();
        if (request.TutorOnly)
        {
            lessons = lessons.Where(lesson => lesson.Agreement.Tutor.UserAccountId == request.AccountId);
        }
        else
        {
            lessons = lessons.Where(lesson => lesson.Agreement.Student.UserAccountId == request.AccountId);
        }

        if (request.AgreementId is { } agreementId)
        {
            var id = new TutoringAgreementId(agreementId);
            lessons = lessons.Where(lesson => lesson.TutoringAgreementId == id);
        }

        var now = clock.GetUtcNow();
        if (request.View == "upcoming")
        {
            lessons = lessons.Where(lesson => lesson.Status == LessonStatus.Scheduled && lesson.TimeSlot.EndsAtUtc > now);
        }
        else if (request.View == "past")
        {
            lessons = lessons.Where(lesson => lesson.TimeSlot.EndsAtUtc <= now || lesson.Status != LessonStatus.Scheduled);
        }

        var total = await lessons.CountAsync(cancellationToken);
        var lastPage = Math.Max(1, (int)Math.Ceiling(total / (double)request.PageSize));
        var page = Math.Min(request.Page, lastPage);
        IOrderedQueryable<Lesson> ordered;
        if (request.View == "upcoming")
        {
            ordered = lessons.OrderBy(lesson => lesson.TimeSlot.StartsAtUtc).ThenBy(lesson => lesson.Id);
        }
        else
        {
            ordered = lessons.OrderByDescending(lesson => lesson.TimeSlot.StartsAtUtc).ThenBy(lesson => lesson.Id);
        }

        var items = await ordered.Skip((page - 1) * request.PageSize).Take(request.PageSize)
            .Select(lesson => new LessonListItemResponse(lesson.Id.Value, lesson.TutoringAgreementId.Value,
                lesson.Agreement.Subject.Name, lesson.PlannedTopic, lesson.TimeSlot.StartsAtUtc, lesson.TimeSlot.EndsAtUtc,
                lesson.Status.ToString(), lesson.CancellationReason == null ? null : lesson.CancellationReason.Text,
                lesson.Agreement.Student.DisplayName.Value, lesson.Agreement.Tutor.Account.Profile.FirstName,
                lesson.Agreement.Tutor.Account.Profile.LastName, new List<LessonListChargeResponse>()))
            .ToListAsync(cancellationToken);

        var lessonIds = items.Select(item => new LessonId(item.LessonId)).ToArray();
        var charges = await (
            from charge in db.LessonCharges.AsNoTracking()
            join account in db.BillingAccounts.AsNoTracking() on charge.BillingAccountId equals account.Id
            join lesson in lessons on charge.LessonId equals lesson.Id
            where lessonIds.Contains(lesson.Id) && charge.Status == ChargeStatus.Active
                && account.TutoringAgreementId == lesson.TutoringAgreementId
            select new LessonListChargeResponse(charge.Id.Value, lesson.Id.Value,
                charge.Amount.Amount, charge.Amount.Currency.Code,
                charge.Payment == null ? null : charge.Payment.PaidAtUtc)
        ).ToListAsync(cancellationToken);

        var result = items.Select(item => item with
        {
            Charges = charges.Where(charge => charge.LessonId == item.LessonId).ToArray()
        }).ToArray();
        logger.LogInformation("Lesson page read. AccountId: {AccountId}, TutorOnly: {TutorOnly}, Page: {Page}, View: {View}, Count: {Count}, RequestMetadata: {RequestMetadata}",
            request.AccountId.Value, request.TutorOnly, page, request.View, result.Length, request.RequestMetadata);
        return new LessonPageResponse(result, page, request.PageSize, total);
    }
}
