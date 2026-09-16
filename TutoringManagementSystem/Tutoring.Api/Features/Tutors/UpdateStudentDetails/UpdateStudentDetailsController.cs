using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.UpdateStudentDetails;

[ApiController]
[Authorize(Roles = "Tutor")]
[Route("api/tutors/students")]
public sealed class UpdateStudentDetailsController(
    ISender sender,
    ILogger<UpdateStudentDetailsController> logger)
    : ControllerBase
{
    [HttpPut("{studentId:guid}/details")]
    public async Task<IActionResult> UpdateStudentDetails(
        Guid studentId,
        [FromBody] UpdateStudentDetailsRequest request,
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
            "Student details update requested. UserId: {UserId}, StudentId: {StudentId}, RequestMetadata: {RequestMetadata}",
            userAccountId,
            studentId,
            metadata);

        await sender.Send(
            new UpdateStudentDetailsCommand(
                new UserAccountId(userAccountId),
                studentId,
                request.Subject,
                request.HourlyRate,
                request.ContactEmail,
                request.ContactPhoneNumber,
                request.Notes,
                metadata),
            cancellationToken);

        return NoContent();
    }
}
