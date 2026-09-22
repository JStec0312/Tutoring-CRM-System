using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.RescheduleLesson;

[ApiController]
[Authorize(Roles = "Tutor")]
[Route("api/tutors/me/lessons")]
public sealed class RescheduleLessonController(
    ISender sender,
    ILogger<RescheduleLessonController> logger)
    : ControllerBase
{
    [HttpPut("{lessonId:guid}/schedule")]
    public async Task<IActionResult> RescheduleLesson(
        Guid lessonId,
        [FromBody] RescheduleLessonRequest request,
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
            "Lesson rescheduling requested. UserId: {UserId}, LessonId: {LessonId}, StartsAt: {StartsAt}, DurationMinutes: {DurationMinutes}, RequestMetadata: {RequestMetadata}",
            userAccountId,
            lessonId,
            request.StartsAt,
            request.DurationMinutes,
            metadata);

        await sender.Send(
            new RescheduleLessonCommand(
                new UserAccountId(userAccountId),
                lessonId,
                request.StartsAt,
                request.DurationMinutes,
                metadata),
            cancellationToken);

        return NoContent();
    }
}