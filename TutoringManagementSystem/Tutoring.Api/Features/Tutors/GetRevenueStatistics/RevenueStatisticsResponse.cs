namespace Tutoring.Api.Features.Tutors.GetRevenueStatistics;

public sealed record RevenueStatisticsResponse(IReadOnlyList<CurrencyRevenue> RevenueByCurrency);

public sealed record CurrencyRevenue(string Currency, decimal Weekly, decimal Monthly, decimal Yearly);
