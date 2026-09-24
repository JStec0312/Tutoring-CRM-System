using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Calendar.GetCalendarLessons;

[ApiController]
[Authorize(Roles = "Tutor,Student")]
[Route("api/calendar")]
public sealed class GetCalendarLessonsController(
    ISender sender,
    ILogger<GetCalendarLessonsController> logger)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CalendarLessonResponse>>>
        GetCalendarLessons(
            [FromQuery] GetCalendarLessonsRequest request,
            CancellationToken cancellationToken)
    {
        var subject = User.FindFirst("sub")?.Value;

        var metadata = new RequestMetadata(
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: Request.Headers.UserAgent.ToString(),
            TraceId: HttpContext.TraceIdentifier);

        if (!Guid.TryParse(subject, out var userAccountId))
        {
            logger.LogWarning(
                "Calendar lessons request rejected because authenticated user id is invalid. RequestMetadata: {RequestMetadata}",
                metadata);

            return Unauthorized();
        }

        logger.LogInformation(
            "Calendar lessons requested. UserAccountId: {UserAccountId}, From: {From}, To: {To}, RequestMetadata: {RequestMetadata}",
            userAccountId,
            request.From,
            request.To,
            metadata);

        if (request.From >= request.To)
        {
            logger.LogWarning(
                "Calendar lessons request rejected because date range is invalid. UserAccountId: {UserAccountId}, From: {From}, To: {To}, RequestMetadata: {RequestMetadata}",
                userAccountId,
                request.From,
                request.To,
                metadata);

            return BadRequest();
        }

        var lessons = await sender.Send(
            new GetCalendarLessonsQuery(
                new UserAccountId(userAccountId),
                request.From.ToUniversalTime(),
                request.To.ToUniversalTime(),
                metadata),
            cancellationToken);

        logger.LogInformation(
            "Calendar lessons request completed. UserAccountId: {UserAccountId}, FromUtc: {FromUtc}, ToUtc: {ToUtc}, LessonsCount: {LessonsCount}, RequestMetadata: {RequestMetadata}",
            userAccountId,
            request.From.ToUniversalTime(),
            request.To.ToUniversalTime(),
            lessons.Count,
            metadata);

        return Ok(lessons);
    }
}