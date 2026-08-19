using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace Tutoring.Api.Features.Auth.Login;

[ApiController]
[Route("api/auth/login")]
public sealed class LoginController(
    ISender sender, ILogger<LoginController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Login attempt for email: {Email}", request.Email);
        var command = new LoginCommand(
            Email: request.Email,
            Password: request.Password
        );

        var response = await sender.Send(
            command,
            cancellationToken);
        logger.LogInformation("Login successful for email: {Email}", request.Email);
        return Ok(response);
    }
}