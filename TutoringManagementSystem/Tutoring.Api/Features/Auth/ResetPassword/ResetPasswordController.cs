using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;

namespace Tutoring.Api.Features.Auth.ResetPassword;

[ApiController]
[Route("api/auth/password-reset")]
public sealed class ResetPasswordController(
    ISender sender,
    ILogger<ResetPasswordController> logger)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var metadata = new RequestMetadata(
            IpAddress:
                HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent:
                Request.Headers.UserAgent.ToString(),
            TraceId:
                HttpContext.TraceIdentifier);

        await sender.Send(
            new ResetPasswordCommand(
                request.Token,
                request.NewPassword,
                metadata),
            cancellationToken);

        logger.LogInformation(
            "Password reset completed. RequestMetadata: {RequestMetadata}",
            metadata);

        return NoContent();
    }
}