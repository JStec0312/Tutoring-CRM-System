using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Auth.CreateGoogleChallenge;

[ApiController]
[Authorize]
[Route("api/users/me/google/challenge")]
public sealed class CreateGoogleChallengeController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateGoogleChallengeResponse>> Create(
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) ||
            accountId == Guid.Empty)
        {
            return Unauthorized();
        }

        return await sender.Send(
            new CreateGoogleChallengeCommand(new UserAccountId(accountId)),
            cancellationToken);
    }
}