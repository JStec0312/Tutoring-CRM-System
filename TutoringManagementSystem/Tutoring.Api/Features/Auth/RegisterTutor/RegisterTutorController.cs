using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Tutoring.Api.Features.Auth.RegisterTutor;

[ApiController]
[Route("api/auth/register/tutor")]
public sealed class RegisterTutorController(
    ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<RegisterTutorResponse>> Register(
        [FromBody] RegisterTutorRequest request,
        CancellationToken cancellationToken,
        ILogger<RegisterTutorController> logger)
    {
        logger.LogInformation("Registering new tutor with email: {Email}", request.Email);
        var command = new RegisterTutorCommand(
            Email: request.Email,
            Password: request.Password,
            UserName: request.UserName,
            FirstName: request.FirstName,
            LastName: request.LastName,
            PhoneNumber: request.PhoneNumber
        );

        var response = await sender.Send(
            command,
            cancellationToken);
        logger.LogInformation("Successfully registered new tutor with email: {Email}, UserId: {UserId}", request.Email, response.UserId);
        return Created(
            $"/api/users/{response.UserId}",
            response);
    }
}
