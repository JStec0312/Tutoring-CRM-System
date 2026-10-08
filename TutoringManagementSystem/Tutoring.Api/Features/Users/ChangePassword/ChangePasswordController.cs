using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Users.ChangePassword;

[ApiController]
[Authorize]
[Route("api/users/me/password")]
public sealed class ChangePasswordController(
    ISender sender,
    ILogger<ChangePasswordController> logger)
    : ControllerBase
{
    [HttpPut]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
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
                "Unauthorized password change attempt. UserId: {UserId}, RequestMetadata: {RequestMetadata}",
                subject,
                metadata);
            return Unauthorized();
        }

        logger.LogInformation(
            "Changing password. UserId: {UserId}, RequestMetadata: {RequestMetadata}",
            userAccountId,
            metadata);

        await sender.Send(
            new ChangePasswordCommand(
                new UserAccountId(userAccountId),
                request.CurrentPassword,
                request.NewPassword,
                metadata),
            cancellationToken);

        return NoContent();
    }
}
