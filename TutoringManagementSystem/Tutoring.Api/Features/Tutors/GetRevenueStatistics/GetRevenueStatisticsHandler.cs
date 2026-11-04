using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Billing;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.GetRevenueStatistics;

public sealed class GetRevenueStatisticsHandler(TutoringDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<GetRevenueStatisticsQuery, RevenueStatisticsResponse>
{
    public async Task<RevenueStatisticsResponse> Handle(GetRevenueStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var reportDate = request.EndDate ?? today;
        if (reportDate > today)
            throw new ArgumentException("The endDate cannot be in the future.", "endDate");

        if (request.StartDate is { } startDate && startDate > reportDate)
            throw new ArgumentException("The startDate cannot be after the report end date.", "startDate");

        var dayStart = new DateTimeOffset(reportDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var through = reportDate == today ? now : dayStart.AddDays(1).AddTicks(-1);
        var daysSinceMonday = ((int)dayStart.DayOfWeek + 6) % 7;
        var weekStart = dayStart.AddDays(-daysSinceMonday);
        var monthStart = new DateTimeOffset(reportDate.Year, reportDate.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var yearStart = new DateTimeOffset(reportDate.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var earliestStart = weekStart < yearStart ? weekStart : yearStart;

        if (request.StartDate is { } fromDate)
        {
            var from = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            if (from > earliestStart)
                earliestStart = from;
        }

        var ownedPayments =
            from payment in dbContext.Payments.AsNoTracking()
            join account in dbContext.BillingAccounts on payment.BillingAccountId equals account.Id
            join agreement in dbContext.TutoringAgreements on account.TutoringAgreementId equals agreement.Id
            where agreement.Tutor.UserAccountId == request.UserAccountId
                && payment.PaidAtUtc >= earliestStart && payment.PaidAtUtc <= through
            select payment;

        if (request.Currency is not null)
        {
            var currency = new Currency(request.Currency);
            ownedPayments = ownedPayments.Where(payment => payment.Amount.Currency == currency);
        }

        var totals = await (
            from payment in ownedPayments
            group payment by payment.Amount.Currency into payments
            select new
            {
                Currency = payments.Key,
                Weekly = payments.Sum(payment => payment.PaidAtUtc >= weekStart ? payment.Amount.Amount : 0m),
                Monthly = payments.Sum(payment => payment.PaidAtUtc >= monthStart ? payment.Amount.Amount : 0m),
                Yearly = payments.Sum(payment => payment.PaidAtUtc >= yearStart ? payment.Amount.Amount : 0m)
            }).ToListAsync(cancellationToken);

        return new RevenueStatisticsResponse(totals
            .OrderBy(total => total.Currency.Code, StringComparer.Ordinal)
            .Select(total => new CurrencyRevenue(total.Currency.Code, total.Weekly, total.Monthly, total.Yearly))
            .ToArray());
    }
}
