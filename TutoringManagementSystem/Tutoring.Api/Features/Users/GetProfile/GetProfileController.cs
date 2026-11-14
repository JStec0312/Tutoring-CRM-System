using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Users.GetProfile;

[ApiController]
[Authorize]
[Route("api/users/me/profile")]
public sealed class GetProfileController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> Get(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
            return Unauthorized();

        return Ok(await sender.Send(new GetProfileQuery(new UserAccountId(accountId)), cancellationToken));
    }
}
