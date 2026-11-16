using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Auth.CompleteGoogleAccountLink;

[ApiController]
[Authorize]
[Route("api/auth/google/link")]
public sealed class CompleteGoogleAccountLinkController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Link(
        [FromBody] CompleteGoogleAccountLinkRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
            return Unauthorized();

        await sender.Send(
            new CompleteGoogleAccountLinkCommand(new UserAccountId(accountId), request.ContinuationToken),
            cancellationToken);
        return NoContent();
    }
}
