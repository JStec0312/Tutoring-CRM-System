using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Auth.SignOutAll;

[ApiController]
[Authorize]
[Route("api/auth/sign-out-all")]
public sealed class SignOutAllController(
    ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SignOutAll(
        CancellationToken cancellationToken,
        ILogger<SignOutAllController> logger)
    {
        var subject = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subject, out var userAccountId))
        {
            return Unauthorized();
        }

        var metadata = new RequestMetadata(
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: Request.Headers.UserAgent.ToString(),
            TraceId: HttpContext.TraceIdentifier);

        await sender.Send(
            new SignOutAllCommand(
                new UserAccountId(userAccountId),
                metadata),
            cancellationToken);

        return NoContent();
    }
}
