using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.UpdateStudentHourlyRate;

[ApiController]
[Authorize(Roles = "Tutor")]
[Route("api/tutors/students")]
public sealed class UpdateStudentHourlyRateController(
    ISender sender,
    ILogger<UpdateStudentHourlyRateController> logger)
    : ControllerBase
{
    [HttpPut("{studentId:guid}/hourly-rate")]
    public async Task<IActionResult> UpdateStudentHourlyRate(
        Guid studentId,
        [FromBody] UpdateStudentHourlyRateRequest request,
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
            "Student hourly rate update requested. UserId: {UserId}, StudentId: {StudentId}, RequestMetadata: {RequestMetadata}",
            userAccountId,
            studentId,
            metadata);

        await sender.Send(
            new UpdateStudentHourlyRateCommand(
                new UserAccountId(userAccountId),
                studentId,
                request.HourlyRate,
                metadata),
            cancellationToken);

        return NoContent();
    }
}
