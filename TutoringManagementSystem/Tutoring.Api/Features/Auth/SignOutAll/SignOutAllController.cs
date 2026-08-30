using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication;

namespace Tutoring.Api.Features.Auth.SignOutAll;

[ApiController]
[Authorize]
[Route("api/auth/sign-out-all")]
public sealed class SignOutAllController(
    ISender sender
    , ILogger<SignOutAllController> logger,
    IOptions<RefreshTokenOptions> refreshTokenOptions
    ) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SignOutAll(
        CancellationToken cancellationToken)
    {
        var subject = User.FindFirst("sub")?.Value;
        
        if (!Guid.TryParse(subject, out var userAccountId))
        {
            return Unauthorized();
        }
        logger.LogInformation("Signing out all sessions for user {UserAccountId}", userAccountId);

        var metadata = new RequestMetadata(
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: Request.Headers.UserAgent.ToString(),
            TraceId: HttpContext.TraceIdentifier);

        await sender.Send(
            new SignOutAllCommand(
                new UserAccountId(userAccountId),
                metadata),
            cancellationToken);

        Response.Cookies.Delete(
            refreshTokenOptions.Value.RefreshTokenCookieName,
            new CookieOptions
            {
                HttpOnly = refreshTokenOptions.Value.HttpOnlyRefreshTokenCookie,
                Secure = refreshTokenOptions.Value.SecureRefreshTokenCookie,
                SameSite = ParseSameSite(refreshTokenOptions.Value.SameSiteRefreshTokenCookie),
                Path = refreshTokenOptions.Value.RefreshTokenPath
            });
        logger.LogInformation("Deleted refresh token cookie for user {UserAccountId}", userAccountId);
        return NoContent();
    }

        private static SameSiteMode ParseSameSite(string sameSite) => sameSite switch
    {
        "Strict" => SameSiteMode.Strict,
        "Lax" => SameSiteMode.Lax,
        "None" => SameSiteMode.None,
        _ => throw new InvalidOperationException($"Invalid SameSite value: {sameSite}")
    };
}