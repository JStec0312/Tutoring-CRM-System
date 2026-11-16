using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Auth.LinkGoogleAccount;

[ApiController]
[Authorize]
[Route("api/users/me/google")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class LinkGoogleAccountController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Consumes("application/json")]
    public async Task<IActionResult> Link(
        LinkGoogleAccountRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) ||
            accountId == Guid.Empty)
        {
            return Unauthorized();
        }

        await sender.Send(new LinkGoogleAccountCommand(
            new UserAccountId(accountId), request.IdToken), cancellationToken);
        return NoContent();
    }
}
