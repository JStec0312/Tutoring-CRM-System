using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace Tutoring.Api.Features.Auth.RegisterStudent;

[ApiController]
[Route("api/auth/register/student")]
public sealed class RegisterStudentController(
    ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<RegisterStudentResponse>> Register(
        [FromBody] RegisterStudentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterStudentCommand(
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

        return Created(
            $"/api/users/{response.UserId}",
            response);
    }
}