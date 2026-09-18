using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.ScheduleLesson;

[ApiController]
[Authorize(Roles = "Tutor")]
[Route("api/tutors/me/lessons")]
public sealed class ScheduleLessonController(
    ISender sender,
    ILogger<ScheduleLessonController> logger)
    : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ScheduleLessonResponse>> ScheduleLesson(
        [FromBody] ScheduleLessonRequest request,
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
            "Lesson scheduling requested. UserId: {UserId}, TutoringAgreementId: {TutoringAgreementId}, StartsAt: {StartsAt}, DurationMinutes: {DurationMinutes}, RequestMetadata: {RequestMetadata}",
            userAccountId,
            request.TutoringAgreementId,
            request.StartsAt,
            request.DurationMinutes,
            metadata);

        var result = await sender.Send(
            new ScheduleLessonCommand(
                new UserAccountId(userAccountId),
                request.TutoringAgreementId,
                request.StartsAt,
                request.DurationMinutes,
                metadata),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            result);
    }
}
