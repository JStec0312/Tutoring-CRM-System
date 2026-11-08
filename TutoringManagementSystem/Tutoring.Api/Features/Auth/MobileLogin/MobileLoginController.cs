using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Auth.Login;
using Tutoring.Api.Features.Auth.Mobile;
using Tutoring.Api.Features.Common.Http;

namespace Tutoring.Api.Features.Auth.MobileLogin;

[ApiController]
[Route(MobileAuthRoutes.Login)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class MobileLoginController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<MobileSessionResponse>> Login(
        [FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var metadata = new RequestMetadata(
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(), HttpContext.TraceIdentifier);
        var result = await sender.Send(
            new LoginCommand(request.Email, request.Password, metadata), cancellationToken);

        return Ok(new MobileSessionResponse(result.AccessToken, result.ExpiresAt,
            result.RefreshToken, result.RefreshTokenExpiresAt));
    }
}
