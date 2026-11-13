using MediatR;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.GetRevenueStatistics;

public sealed record GetRevenueStatisticsQuery(UserAccountId UserAccountId, string? Currency,
    DateOnly? StartDate, DateOnly? EndDate)
    : IRequest<RevenueStatisticsResponse>;
