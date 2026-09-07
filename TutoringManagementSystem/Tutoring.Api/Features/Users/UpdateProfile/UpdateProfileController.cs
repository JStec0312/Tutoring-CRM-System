using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Users.UpdateProfile;

[ApiController]
[Authorize]
[Route("api/users/me/profile")]
public sealed class UpdateProfileController(
    ISender sender, ILogger<UpdateProfileController> logger)
    : ControllerBase
{
    [HttpPut]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var subject = User.FindFirst("sub")?.Value;
        var metadata = new RequestMetadata(
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: Request.Headers.UserAgent.ToString(),
            TraceId: HttpContext.TraceIdentifier);
        logger.LogInformation(
            "Updating profile. UserId: {UserId}, RequestMetadata: {RequestMetadata}",
            subject,
            metadata);
        
        if (!Guid.TryParse(subject, out var userAccountId))
        {
            logger.LogWarning(
                "Unauthorized profile update attempt. UserId: {UserId}, RequestMetadata: {RequestMetadata}",
                subject,
                metadata);
            return Unauthorized();
        }

        await sender.Send(
            new UpdateProfileCommand(
                new UserAccountId(userAccountId),
                request.UserName,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                metadata),
            cancellationToken);

        return NoContent();
    }
}