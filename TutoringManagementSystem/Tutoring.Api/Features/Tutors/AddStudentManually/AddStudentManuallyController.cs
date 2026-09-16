using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.AddStudentManually;

[ApiController]
[Authorize(Roles = "Tutor")]
[Route("api/tutors/me/students")]
public sealed class AddStudentManuallyController(
    ISender sender,
    ILogger<AddStudentManuallyController> logger)
    : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AddStudentManuallyResponse>> AddStudent(
        [FromBody] AddStudentManuallyRequest request,
        CancellationToken cancellationToken)
    {
        var subject = User.FindFirst("sub")?.Value;

        var metadata = new RequestMetadata(
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: Request.Headers.UserAgent.ToString(),
            TraceId: HttpContext.TraceIdentifier);

        logger.LogInformation(
            "Manual student creation requested. UserId: {UserId}, DisplayName: {DisplayName}, RequestMetadata: {RequestMetadata}",
            subject,
            request.DisplayName,
            metadata);

        if (!Guid.TryParse(subject, out var userAccountId))
        {
            return Unauthorized();
        }

        var result = await sender.Send(
            new AddStudentManuallyCommand(
                new UserAccountId(userAccountId),
                request.DisplayName,
                request.Title,
                request.Subject,
                request.HourlyRate,
                metadata),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            result);
    }
}