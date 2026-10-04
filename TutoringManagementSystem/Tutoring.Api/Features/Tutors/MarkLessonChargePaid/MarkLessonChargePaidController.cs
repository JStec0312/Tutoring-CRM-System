using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.MarkLessonChargePaid;

[ApiController]
[Authorize(Roles = "Tutor")]
[Route("api/tutors/me/lesson-charges")]
public sealed class MarkLessonChargePaidController(
    ISender sender,
    ILogger<MarkLessonChargePaidController> logger) : ControllerBase
{
    [HttpPost("{lessonChargeId:guid}/mark-paid")]
    public async Task<IActionResult> MarkPaid(
        Guid lessonChargeId,
        [FromBody] MarkLessonChargePaidRequest request,
        CancellationToken cancellationToken)
    {
        var subject = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subject, out var userAccountId))
        {
            return Unauthorized();
        }

        var metadata = new RequestMetadata(
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            HttpContext.TraceIdentifier);

        logger.LogInformation(
            "Lesson charge payment requested. UserId: {UserId}, LessonChargeId: {LessonChargeId}, RequestMetadata: {RequestMetadata}",
            userAccountId,
            lessonChargeId,
            metadata);

        await sender.Send(
            new MarkLessonChargePaidCommand(
                new UserAccountId(userAccountId),
                lessonChargeId,
                request.PaidAtUtc,
                request.Reference,
                metadata),
            cancellationToken);

        return NoContent();
    }
}
