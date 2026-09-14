using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Api.Features.Students.AcceptStudentInvitation;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Students.AcceptStudentInvitation;

[ApiController]
[Authorize(Roles = "Student")]
[Route("api/student-invitations")]
public sealed class AcceptStudentInvitationController(
    ISender sender,
    ILogger<AcceptStudentInvitationController> logger)
    : ControllerBase
{
    [HttpPost("{token}/accept")]
    public async Task<ActionResult<AcceptStudentInvitationResponse>>
        AcceptStudentInvitation(
            [FromRoute] string token,
            CancellationToken cancellationToken)
    {
        var subject =
            User.FindFirst("sub")?.Value;

        var metadata = new RequestMetadata(
            IpAddress:
                HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent:
                Request.Headers.UserAgent.ToString(),
            TraceId:
                HttpContext.TraceIdentifier);

        logger.LogInformation(
            "Student invitation acceptance requested. UserId: {UserId}, RequestMetadata: {RequestMetadata}",
            subject,
            metadata);

        if (!Guid.TryParse(
                subject,
                out var userAccountId))
        {
            logger.LogWarning(
                "Unauthorized student invitation acceptance attempt. UserId: {UserId}, RequestMetadata: {RequestMetadata}",
                subject,
                metadata);

            return Unauthorized();
        }

        var response = await sender.Send(
            new AcceptStudentInvitationCommand(
                new UserAccountId(
                    userAccountId),
                token,
                metadata),
            cancellationToken);

        return Ok(response);
    }
}