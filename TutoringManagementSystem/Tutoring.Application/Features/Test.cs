using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tutoring.Api.Features.Auth.TestAuthentication;

[ApiController]
[Route("api/auth/test")]
public sealed class TestAuthenticationController
    : ControllerBase
{
    [Authorize]
    [HttpGet]
    public IActionResult Test()
    {
        return Ok(new
        {
            Message = "You are authenticated.",
            Claims = User.Claims.Select(claim => new
            {
                claim.Type,
                claim.Value
            })
        });
    }
}