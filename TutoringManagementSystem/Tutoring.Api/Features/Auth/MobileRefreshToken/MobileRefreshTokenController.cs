using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Auth.Mobile;
using Tutoring.Api.Features.Auth.RefreshToken;
using Tutoring.Api.Features.Common.Http;

namespace Tutoring.Api.Features.Auth.MobileRefreshToken;

[ApiController]
[Route(MobileAuthRoutes.Refresh)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class MobileRefreshTokenController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<MobileSessionResponse>> Refresh(
        [FromBody] MobileRefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new InvalidRefreshTokenException();

        var metadata = new RequestMetadata(
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(), HttpContext.TraceIdentifier);
        var result = await sender.Send(
            new RefreshTokenCommand(request.RefreshToken, metadata), cancellationToken);

        return Ok(new MobileSessionResponse(result.AccessToken, result.ExpiresAt,
            result.RefreshToken, result.RefreshTokenExpiresAt));
    }
}
