using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.GetTotalTeachingTime;

[ApiController]
[Authorize(Roles = "Tutor")]
[Route("api/tutors/me/lessons")]
public sealed class GetTotalTeachingTimeController(ISender sender) : ControllerBase
{
    [HttpGet("total-teaching-time")]
    public async Task<ActionResult<TotalTeachingTimeResponse>> Get(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
            return Unauthorized();

        return Ok(await sender.Send(new GetTotalTeachingTimeQuery(new UserAccountId(accountId)),
            cancellationToken));
    }
}
