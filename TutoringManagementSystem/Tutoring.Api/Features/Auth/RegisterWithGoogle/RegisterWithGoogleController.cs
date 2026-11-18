using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tutoring.Api.Features.Auth.Login;
using Tutoring.Api.Features.Auth.SignInWithGoogle;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Infrastructure.Authentication.RefreshTokens;

namespace Tutoring.Api.Features.Auth.RegisterWithGoogle;

[ApiController]
[Route("api/auth/google/register")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RegisterWithGoogleController(ISender sender, IOptions<RefreshTokenOptions> refreshTokenOptions)
    : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<SignInWithGoogleResponse>> Register(
        [FromBody] RegisterWithGoogleRequest request, CancellationToken cancellationToken)
    {
        var metadata = new RequestMetadata(
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            HttpContext.TraceIdentifier);
        var session = await sender.Send(new RegisterWithGoogleCommand(
            request.ContinuationToken, request.Role, request.UserName, request.FirstName,
            request.LastName, request.PhoneNumber, metadata), cancellationToken);

        if (!request.UseNativeSession)
        {
            LoginRefreshTokenCookie.Append(Response, refreshTokenOptions.Value, session);
        }

        return StatusCode(StatusCodes.Status201Created, SignInWithGoogleResponse.FromSession(session, request.UseNativeSession));
    }
}
