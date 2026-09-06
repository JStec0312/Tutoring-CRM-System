using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;

namespace Tutoring.Api.Features.Auth.ConfirmEmail;

[ApiController]
[Route("api/auth/email")]
public sealed class ConfirmEmailController(
    ISender sender,
    ILogger<ConfirmEmailController> logger) : ControllerBase
{
    [HttpGet("confirm")]
    public async Task<IActionResult> Confirm(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        RequestMetadata requestMetadata = new RequestMetadata(
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.Request.Headers["User-Agent"].ToString(),
            HttpContext.TraceIdentifier
        );
        logger.LogInformation("Received email confirmation request for token: {Token} from IP: {IpAddress} with User-Agent: {UserAgent} and TraceId: {TraceId}", token, requestMetadata.IpAddress, requestMetadata.UserAgent, requestMetadata.TraceId);
        await sender.Send(
            new ConfirmEmailCommand(token, requestMetadata),
            cancellationToken);

            return Ok(new
        {
            message = "Email confirmed successfully."
        });
    }
}