using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.SetLessonStatus;

[ApiController]
[Authorize(Roles = "Tutor")]
[Route("api/tutors/me/lessons")]
public sealed class SetLessonStatusController(
    ISender sender,
    ILogger<SetLessonStatusController> logger)
    : ControllerBase
{
    [HttpPut("{lessonId:guid}/status")]
    public async Task<IActionResult> SetLessonStatus(
        Guid lessonId,
        [FromBody] SetLessonStatusRequest request,
        CancellationToken cancellationToken)
    {
        var subject = User.FindFirst("sub")?.Value;

        var metadata = new RequestMetadata(
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: Request.Headers.UserAgent.ToString(),
            TraceId: HttpContext.TraceIdentifier);

        if (!Guid.TryParse(subject, out var userAccountId))
        {
            return Unauthorized();
        }

        logger.LogInformation(
            "Lesson status change requested. UserId: {UserId}, LessonId: {LessonId}, Status: {Status}, RequestMetadata: {RequestMetadata}",
            userAccountId,
            lessonId,
            request.Status,
            metadata);

        await sender.Send(
            new SetLessonStatusCommand(
                new UserAccountId(userAccountId),
                lessonId,
                request.Status,
                metadata),
            cancellationToken);

        return NoContent();
    }
}