using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tutoring.Api.Features.Auth.Login;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Infrastructure.Authentication.RefreshTokens;

namespace Tutoring.Api.Features.Auth.SignInWithGoogle;

[ApiController]
[Route("api/auth/google/sign-in")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SignInWithGoogleController(ISender sender, IOptions<RefreshTokenOptions> refreshTokenOptions)
    : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [Consumes("application/json")]
    [RequestSizeLimit(16_384)]
    public async Task<ActionResult<SignInWithGoogleResponse>> SignIn(
        [FromBody] SignInWithGoogleRequest request, CancellationToken cancellationToken)
    {
        var metadata = new RequestMetadata(
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            HttpContext.TraceIdentifier);
        // Check the handler result to determine the next step in the sign-in process.
        var result = await sender.Send(new SignInWithGoogleCommand(request.IdToken, metadata), cancellationToken);

        // Client needs to link existing account or complete registration if continuation is required.
        if (result is SignInWithGoogleResult.Continue continuation)
        {
            return Ok(new SignInWithGoogleResponse(
                Status: continuation.RequiresLink ? "linkRequired" : "registrationRequired",
                ContinuationToken: continuation.Token,
                ContinuationExpiresAtUtc: continuation.ExpiresAtUtc));
        }

        // If continuation is not required, the user has successfully signed in.
        var session = ((SignInWithGoogleResult.SignedIn)result).Session;

        // Append the refresh token to the response if native session is not used.
        // mobile clients recieve session via response
        // in a browser the usenativesession is set to false so 
        // a refresh token cookie is appended to the response.
        if (!request.UseNativeSession)
            LoginRefreshTokenCookie.Append(Response, refreshTokenOptions.Value, session);

        return Ok(new SignInWithGoogleResponse(
            Status: "signedIn",
            AccessToken: session.AccessToken,
            ExpiresAt: session.ExpiresAt,
            RefreshToken: request.UseNativeSession ? session.RefreshToken : null,
            RefreshTokenExpiresAt: request.UseNativeSession ? session.RefreshTokenExpiresAt : null));
    }
}
