using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Infrastructure.Authentication;

namespace Tutoring.Api.Features.Auth.SignOut;

[ApiController]
[Route("api/auth/sign-out")]
public sealed class SignOutController(
    ISender sender,
    IOptions<RefreshTokenOptions> refreshTokenOptions,
    ILogger<SignOutController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SignOut(
        CancellationToken cancellationToken)
    {
    
        var metadata = new RequestMetadata(
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: Request.Headers.UserAgent.ToString(),
            TraceId: HttpContext.TraceIdentifier);
        logger.LogInformation(
            "Sign out request received. IP: {IpAddress}, TraceId: {TraceId}",
            metadata.IpAddress,
            metadata.TraceId);    
        await sender.Send(
            new SignOutCommand(
                Request.Cookies[refreshTokenOptions.Value.RefreshTokenCookieName],
                metadata),
            cancellationToken);

        DeleteRefreshTokenCookie();
        
        return NoContent();
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(
            refreshTokenOptions.Value.RefreshTokenCookieName,
            new CookieOptions
            {
                HttpOnly = refreshTokenOptions.Value.HttpOnlyRefreshTokenCookie,
                Secure = refreshTokenOptions.Value.SecureRefreshTokenCookie,
                SameSite = ParseSameSite(refreshTokenOptions.Value.SameSiteRefreshTokenCookie),
                Path = refreshTokenOptions.Value.RefreshTokenPath
            });
    }

    private static SameSiteMode ParseSameSite(string sameSite) => sameSite switch
    {
        "Strict" => SameSiteMode.Strict,
        "Lax" => SameSiteMode.Lax,
        "None" => SameSiteMode.None,
        _ => throw new InvalidOperationException($"Invalid SameSite value: {sameSite}")
    };
}
