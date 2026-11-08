using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Auth.Mobile;
using Tutoring.Api.Features.Auth.SignOut;
using Tutoring.Api.Features.Common.Http;

namespace Tutoring.Api.Features.Auth.MobileSignOut;

[ApiController]
[Route(MobileAuthRoutes.SignOut)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class MobileSignOutController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SignOut(
        [FromBody] MobileRefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var metadata = new RequestMetadata(
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(), HttpContext.TraceIdentifier);
        await sender.Send(new SignOutCommand(request.RefreshToken, metadata), cancellationToken);
        return NoContent();
    }
}
