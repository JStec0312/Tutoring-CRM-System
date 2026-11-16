using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Tutoring.Api.Features.Auth.CreateGoogleSignInChallenge;

[ApiController]
[Route("api/auth/google/challenge")]
public sealed class CreateGoogleSignInChallengeController(ISender sender) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<CreateGoogleSignInChallengeResponse>> Create(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CreateGoogleSignInChallengeRequest? _,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(new CreateGoogleSignInChallengeCommand(), cancellationToken);
        return Ok(response);
    }
}
