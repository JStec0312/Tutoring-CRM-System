using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Tutoring.Api.Features.Common.Http;

namespace Tutoring.Api.Features.Auth.RefreshToken;


[ApiController]
[Route("api/auth/refresh")]
public sealed class RefreshController(
    ISender sender,
    IOptions<RefreshTokenOptions> refreshTokenOptions,
    ILogger<RefreshController> logger
    )
    : ControllerBase
{
    private readonly RefreshTokenOptions _options =
        refreshTokenOptions.Value;

    [HttpPost]
    public async Task<ActionResult<RefreshTokenResponse>> Refresh(
        CancellationToken cancellationToken)
    {   
        logger.LogInformation(
            "Refreshing access token. IP: {IpAddress}, UserAgent: {UserAgent}, TraceId: {TraceId}",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            HttpContext.TraceIdentifier);
        
        var refreshToken =
            Request.Cookies[
                _options.RefreshTokenCookieName];
            logger.LogInformation(
            "Refresh token cookie generated for  IP: {IpAddress}, UserAgent: {UserAgent}, TraceId: {TraceId}",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            HttpContext.TraceIdentifier);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new InvalidRefreshTokenException();
        }
        RequestMetadata metadata = new RequestMetadata(
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: Request.Headers.UserAgent.ToString(),
            TraceId: HttpContext.TraceIdentifier);
        var command = new RefreshTokenCommand(
            RefreshToken: refreshToken,
            Metadata: metadata);

        RefreshTokenHandlerResult result = await sender.Send(
            command,
            cancellationToken);

        Response.Cookies.Append(
            _options.RefreshTokenCookieName,
            result.RefreshToken,
            new CookieOptions
            {
                HttpOnly =
                    _options.HttpOnlyRefreshTokenCookie,

                Secure =
                    _options.SecureRefreshTokenCookie,

                SameSite = SameSiteMode.Strict,

                Expires =
                    result.RefreshTokenExpiresAt,

                Path =
                    _options.RefreshTokenPath
            });
        logger.LogInformation(
            "Successfully refreshed access token. IP: {IpAddress}, UserAgent: {UserAgent}, TraceId: {TraceId}",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            HttpContext.TraceIdentifier);
        return Ok(new RefreshTokenResponse(
            AccessToken: result.AccessToken,
            ExpiresAt: result.ExpiresAt));
    }
}