using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.GetRevenueStatistics;

[ApiController]
[Authorize(Roles = "Tutor")]
[Route("api/tutors/me/statistics")]
public sealed class GetRevenueStatisticsController(ISender sender) : ControllerBase
{
    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueStatisticsResponse>> Get(
        [FromQuery, RegularExpression("^[A-Za-z]{3}$")] string? currency,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
            return Unauthorized();

        return Ok(await sender.Send(new GetRevenueStatisticsQuery(new UserAccountId(accountId), currency, startDate, endDate),
            cancellationToken));
    }
}
