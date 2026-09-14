using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.InviteStudent;

[ApiController]
[Authorize(Roles = "Tutor")]
[Route("api/tutors/me/student-invitations")]
public sealed class InviteStudentController(
    ISender sender,
    ILogger<InviteStudentController> logger)
    : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<InviteStudentResponse>> InviteStudent(
        [FromBody] InviteStudentRequest request,
        CancellationToken cancellationToken)
    {
        var subject = User.FindFirst("sub")?.Value;

        var metadata = new RequestMetadata(
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: Request.Headers.UserAgent.ToString(),
            TraceId: HttpContext.TraceIdentifier);

        logger.LogInformation(
            "Student invitation requested. UserId: {UserId}, Recipient: {Recipient}, RequestMetadata: {RequestMetadata}",
            subject,
            request.Email,
            metadata);

        if (!Guid.TryParse(subject, out var userAccountId))
        {
            logger.LogWarning(
                "Unauthorized student invitation attempt. UserId: {UserId}, Recipient: {Recipient}, RequestMetadata: {RequestMetadata}",
                subject,
                request.Email,
                metadata);

            return Unauthorized();
        }

        var result = await sender.Send(
            new InviteStudentCommand(
                new UserAccountId(userAccountId),
                request.Email,
                request.Title,
                request.Subject,
                request.HourlyRate,
                metadata),
            cancellationToken);

        return Ok(result);
    }
}