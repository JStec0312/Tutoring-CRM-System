using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;
using Tutoring.Domain.Students;

namespace Tutoring.Api.Features.Tutors.GetStudentStatistics;

[ApiController]
[Authorize(Roles = nameof(UserRole.Tutor))]
[Route("api/tutors/me/students")]
public sealed class GetStudentStatisticsController(ISender sender) : ControllerBase
{
    [HttpGet("{studentId:guid}/statistics")]
    public async Task<ActionResult<StudentStatisticsResponse>> Get(
        Guid studentId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
        {
            return Unauthorized();
        }

        var metadata = new RequestMetadata(
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: Request.Headers.UserAgent.ToString(),
            TraceId: HttpContext.TraceIdentifier);

        return Ok(await sender.Send(
            new GetStudentStatisticsQuery(new UserAccountId(accountId), new StudentId(studentId), from, to, metadata),
            cancellationToken));
    }
}
