using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.GetStudentLessonHistory;

[ApiController]
[Authorize(Roles = "Tutor")]
[Route("api/tutors/students")]
public sealed class GetStudentLessonHistoryController(
    ISender sender,
    ILogger<GetStudentLessonHistoryController> logger)
    : ControllerBase
{
    [HttpGet("{studentId:guid}/lessons/history")]
    public async Task<ActionResult<IReadOnlyCollection<StudentLessonHistoryResponse>>>
        GetStudentLessonHistory(
            Guid studentId,
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
                "Student lesson history request rejected because authenticated user id is invalid. StudentId: {StudentId}, RequestMetadata: {RequestMetadata}",
                studentId,
                metadata);

            return Unauthorized();
        }

        logger.LogInformation(
            "Student lesson history requested. StudentId: {StudentId}, UserAccountId: {UserAccountId}, RequestMetadata: {RequestMetadata}",
            studentId,
            userAccountId,
            metadata);

        var lessons = await sender.Send(
            new GetStudentLessonHistoryQuery(
                new UserAccountId(userAccountId),
                studentId,
                metadata),
            cancellationToken);

        logger.LogInformation(
            "Student lesson history request completed. StudentId: {StudentId}, UserAccountId: {UserAccountId}, LessonsCount: {LessonsCount}, RequestMetadata: {RequestMetadata}",
            studentId,
            userAccountId,
            lessons.Count,
            metadata);

        return Ok(lessons);
    }
}
