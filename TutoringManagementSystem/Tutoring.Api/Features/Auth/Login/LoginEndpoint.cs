using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using Tutoring.Api.Features.Common.Http;

namespace Tutoring.Api.Features.Auth.Login;

[ApiController]
[Route("api/auth/login")]
public sealed class LoginController(
    ISender sender, ILogger<LoginController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var metadata = new RequestMetadata(
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: Request.Headers.UserAgent.ToString(),
            TraceId: HttpContext.TraceIdentifier);

        logger.LogInformation("Login attempt for email: {Email}, IP: {IpAddress}, UserAgent: {UserAgent}, TraceId: {TraceId}",
            request.Email,
            metadata.IpAddress,
            metadata.UserAgent,
            metadata.TraceId);
        var command = new LoginCommand(
            Email: request.Email,
            Password: request.Password,
            Metadata: metadata
        );

        var response = await sender.Send(
            command,
            cancellationToken);
        logger.LogInformation("Login successful for email: {Email}, IP: {IpAddress}, TraceId: {TraceId}",
            request.Email,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.TraceIdentifier);
        return Ok(response);
    }
}