using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.GetCompletedLessonCount;

[ApiController]
[Authorize(Roles = nameof(UserRole.Tutor))]
[Route("api/tutors/me/lessons")]
public sealed class GetCompletedLessonCountController(ISender sender) : ControllerBase
{
    [HttpGet("completed-count")]
    public async Task<ActionResult<CompletedLessonCountResponse>> Get(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
            return Unauthorized();

        return Ok(await sender.Send(new GetCompletedLessonCountQuery(new UserAccountId(accountId)),
            cancellationToken));
    }
}
