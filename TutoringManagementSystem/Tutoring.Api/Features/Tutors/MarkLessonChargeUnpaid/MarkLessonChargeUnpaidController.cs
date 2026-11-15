using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.MarkLessonChargeUnpaid;

[ApiController]
[Authorize(Roles = nameof(UserRole.Tutor))]
[Route("api/tutors/me/lesson-charges")]
public sealed class MarkLessonChargeUnpaidController(ISender sender) : ControllerBase
{
    [HttpPost("{lessonChargeId:guid}/mark-unpaid")]
    public async Task<IActionResult> MarkUnpaid(Guid lessonChargeId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
            return Unauthorized();

        var metadata = new RequestMetadata(HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(), HttpContext.TraceIdentifier);
        await sender.Send(new MarkLessonChargeUnpaidCommand(new UserAccountId(accountId),
            new(lessonChargeId), metadata), cancellationToken);
        return NoContent();
    }
}
