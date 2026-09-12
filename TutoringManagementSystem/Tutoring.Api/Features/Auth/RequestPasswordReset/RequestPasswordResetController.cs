using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;

namespace Tutoring.Api.Features.Auth.RequestPasswordReset;

[ApiController]
[Route("api/auth/password-reset/request")]
public sealed class RequestPasswordResetController(
    ISender sender,
    ILogger<RequestPasswordResetController> logger)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RequestPasswordReset(
        [FromBody] RequestPasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        var metadata = new RequestMetadata(
            IpAddress:
                HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent:
                Request.Headers.UserAgent.ToString(),
            TraceId:
                HttpContext.TraceIdentifier);

        logger.LogInformation(
            "Password reset requested. Email: {Email}, RequestMetadata: {RequestMetadata}",
            request.Email,
            metadata);

        await sender.Send(
            new RequestPasswordResetCommand(
                request.Email,
                metadata),
            cancellationToken);


        return NoContent();
    }
}