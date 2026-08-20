using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Infrastructure.Authentication;

namespace Tutoring.Api.Features.Auth.Login;

[ApiController]
[Route("api/auth/login")]
public sealed class LoginController(
    ISender sender, ILogger<LoginController> logger, IOptions<RefreshTokenOptions> refreshTokenOptions) : ControllerBase
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

        LoginHandlerResult response = await sender.Send(
            command,
            cancellationToken);
        Response.Cookies.Append(
            refreshTokenOptions.Value.RefreshTokenCookieName,
            response.RefreshToken,
            new CookieOptions
            {
                HttpOnly = refreshTokenOptions.Value.HttpOnlyRefreshTokenCookie,
                SameSite = refreshTokenOptions.Value.SameSiteRefreshTokenCookie switch
                {
                    "Strict" => SameSiteMode.Strict,
                    "Lax" => SameSiteMode.Lax,
                    "None" => SameSiteMode.None,
                    _ => throw new InvalidOperationException($"Invalid SameSite value: {refreshTokenOptions.Value.SameSiteRefreshTokenCookie}")
                },
                Secure = refreshTokenOptions.Value.SecureRefreshTokenCookie,
                Expires = DateTime.UtcNow.AddDays(refreshTokenOptions.Value.RefreshTokenExpirationDays),
                Path = refreshTokenOptions.Value.RefreshTokenPath
            });
        logger.LogInformation("Login successful for email: {Email}, IP: {IpAddress}, TraceId: {TraceId}",
            request.Email,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.TraceIdentifier);
        return Ok(new LoginResponse(
            AccessToken: response.AccessToken,
            ExpiresAt: response.ExpiresAt
        ));
    }
}