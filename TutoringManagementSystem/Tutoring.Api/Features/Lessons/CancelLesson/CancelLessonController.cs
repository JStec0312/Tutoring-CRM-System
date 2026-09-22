using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Lessons.CancelLesson;

[ApiController]
[Authorize(Roles = "Tutor,Student")]
[Route("api/lessons")]
public sealed class CancelLessonController(
    ISender sender,
    ILogger<CancelLessonController> logger)
    : ControllerBase
{
    [HttpPost("{lessonId:guid}/cancel")]
    public async Task<IActionResult> CancelLesson(
        Guid lessonId,
        [FromBody] CancelLessonRequest request,
        CancellationToken cancellationToken)
    {
        var subject = User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(
                subject,
                out var userAccountId))
        {
            return Unauthorized();
        }

        var metadata = new RequestMetadata(
            IpAddress: HttpContext.Connection
                .RemoteIpAddress?.ToString(),
            UserAgent: Request.Headers
                .UserAgent.ToString(),
            TraceId: HttpContext.TraceIdentifier);

        logger.LogInformation(
            "Lesson cancellation requested. UserId: {UserId}, LessonId: {LessonId}, CancellationParty: {CancellationParty}, RequestMetadata: {RequestMetadata}",
            userAccountId,
            lessonId,
            request.CancellationParty,
            metadata);

        await sender.Send(
            new CancelLessonCommand(
                new UserAccountId(userAccountId),
                lessonId,
                request.CancellationParty,
                request.Reason,
                metadata),
            cancellationToken);

        return NoContent();
    }
}