using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;

namespace Tutoring.Api.Features.Auth.RegisterStudent;

[ApiController]
[Route("api/auth/register/student")]
public sealed class RegisterStudentController(
    ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<RegisterStudentResponse>> Register(
        [FromBody] RegisterStudentRequest request,
        CancellationToken cancellationToken,
        ILogger<RegisterStudentController> logger)
    {

        var metadata = new RequestMetadata(
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: Request.Headers.UserAgent.ToString(),
            TraceId: HttpContext.TraceIdentifier);
        logger.LogInformation("Registering new student with email: {Email}, IP: {IpAddress}, UserAgent: {UserAgent}, TraceId: {TraceId}",
            request.Email,
            metadata.IpAddress,
            metadata.UserAgent,
            metadata.TraceId);
        var command = new RegisterStudentCommand(
            Email: request.Email,
            Password: request.Password,
            UserName: request.UserName,
            FirstName: request.FirstName,
            LastName: request.LastName,
            PhoneNumber: request.PhoneNumber,
            Metadata: metadata
        );

        var response = await sender.Send(
            command,
            cancellationToken);
        logger.LogInformation("Successfully registered new student with email: {Email}, UserId: {UserId}, IP: {IpAddress}, TraceId: {TraceId}",
            request.Email,
            response.UserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.TraceIdentifier);
        return Created(
            $"/api/users/{response.UserId}",
            response);
    }
}