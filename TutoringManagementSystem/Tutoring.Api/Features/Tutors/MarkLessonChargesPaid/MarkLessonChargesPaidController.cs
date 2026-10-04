using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.MarkLessonChargesPaid;

[ApiController]
[Authorize(Roles = "Tutor")]
[Route("api/tutors/me/lesson-charges")]
public sealed class MarkLessonChargesPaidController(
    ISender sender,
    ILogger<MarkLessonChargesPaidController> logger) : ControllerBase
{
    [HttpPost("mark-paid")]
    public async Task<IActionResult> MarkPaid(
        [FromBody] MarkLessonChargesPaidRequest request,
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
            "Lesson charges payment requested. UserId: {UserId}, ChargeCount: {ChargeCount}, RequestMetadata: {RequestMetadata}",
            userAccountId,
            request.LessonChargeIds.Count,
            metadata);

        await sender.Send(
            new MarkLessonChargesPaidCommand(
                new UserAccountId(userAccountId),
                request.LessonChargeIds,
                request.PaidAtUtc,
                request.Reference,
                metadata),
            cancellationToken);

        return NoContent();
    }
}
