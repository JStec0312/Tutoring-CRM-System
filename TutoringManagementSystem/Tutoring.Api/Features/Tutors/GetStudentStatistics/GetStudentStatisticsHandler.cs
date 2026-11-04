using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Common.Authorization;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.Tutors;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.GetStudentStatistics;

public sealed class GetStudentStatisticsHandler(
    TutoringDbContext dbContext,
    ILogger<GetStudentStatisticsHandler> logger)
    : IRequestHandler<GetStudentStatisticsQuery, StudentStatisticsResponse>
{
    public async Task<StudentStatisticsResponse> Handle(
        GetStudentStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.From > request.To)
        {
            throw new ArgumentException("From must be on or before to.", "from");
        }

        var tutorId = await dbContext.Tutors.AsNoTracking()
            .Where(tutor => tutor.UserAccountId == request.UserAccountId)
            .Select(tutor => (TutorId?)tutor.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new TutorNotFoundException(request.UserAccountId.Value);

        logger.LogInformation(
            "Retrieving student statistics. TutorId: {TutorId}, StudentId: {StudentId}, From: {From}, To: {To}, RequestMetadata: {RequestMetadata}",
            tutorId.Value, request.StudentId.Value, request.From, request.To, request.RequestMetadata);

        var agreements = dbContext.TutoringAgreements.AsNoTracking()
            .ForTutor(tutorId)
            .Where(agreement => agreement.StudentId == request.StudentId);

        if (!await agreements.AnyAsync(cancellationToken))
        {
            throw new TutoringAgreementNotFoundException(tutorId.Value, request.StudentId.Value);
        }

        var lessons =
            from lesson in dbContext.Lessons.AsNoTracking()
            join agreement in agreements on lesson.TutoringAgreementId equals agreement.Id
            select lesson;

        if (request.From is { } fromDate)
        {
            var from = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            lessons = lessons.Where(lesson => lesson.TimeSlot.StartsAtUtc >= from);
        }

        if (request.To is { } toDate)
        {
            var to = new DateTimeOffset(toDate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
            lessons = lessons.Where(lesson => lesson.TimeSlot.StartsAtUtc <= to);
        }

        var counts = await lessons.GroupBy(_ => 1)
            .Select(group => new
            {
                Scheduled = group.Count(lesson => lesson.Status == LessonStatus.Scheduled),
                Completed = group.Count(lesson => lesson.Status == LessonStatus.Completed),
                Cancelled = group.Count(lesson => lesson.Status == LessonStatus.Cancelled),
                Missed = group.Count(lesson => lesson.Status == LessonStatus.Missed),
                CompletedSeconds = group.Sum(lesson => lesson.Status == LessonStatus.Completed
                    ? (long)EF.Functions.DateDiffSecond(lesson.TimeSlot.StartsAtUtc, lesson.TimeSlot.EndsAtUtc)
                    : 0L),
                CancelledByStudent = group.Count(lesson => lesson.Status == LessonStatus.Cancelled
                    && lesson.LessonCancellationParty == LessonCancellationParty.Student),
                CancelledByTutor = group.Count(lesson => lesson.Status == LessonStatus.Cancelled
                    && lesson.LessonCancellationParty == LessonCancellationParty.Tutor)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var payments = await (
            from charge in dbContext.LessonCharges.AsNoTracking()
            join lesson in lessons on charge.LessonId equals lesson.Id
            where charge.Status == ChargeStatus.Active
            group charge by charge.Amount.Currency into charges
            select new
            {
                Currency = charges.Key,
                PaidAmount = charges.Sum(charge => charge.PaymentId != null ? charge.Amount.Amount : 0m),
                UnpaidAmount = charges.Sum(charge => charge.PaymentId == null ? charge.Amount.Amount : 0m),
                UnpaidChargeCount = charges.Count(charge => charge.PaymentId == null)
            }).ToListAsync(cancellationToken);

        var completed = counts?.Completed ?? 0;
        var missed = counts?.Missed ?? 0;
        var finalized = completed + missed;

        return new StudentStatisticsResponse(
            new StudentLessonStatistics(counts?.Scheduled ?? 0, completed, counts?.Cancelled ?? 0, missed,
                (counts?.CompletedSeconds ?? 0) / 60m),
            new StudentAttendanceStatistics(finalized == 0 ? null : 100m * completed / finalized,
                counts?.CancelledByStudent ?? 0, counts?.CancelledByTutor ?? 0),
            payments.OrderBy(payment => payment.Currency.Code, StringComparer.Ordinal)
                .Select(payment => new StudentPaymentStatistics(payment.Currency.Code, payment.PaidAmount,
                    payment.UnpaidAmount, payment.UnpaidChargeCount))
                .ToArray());
    }
}
