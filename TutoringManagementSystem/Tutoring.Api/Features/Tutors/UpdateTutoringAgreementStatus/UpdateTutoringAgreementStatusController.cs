using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.UpdateTutoringAgreementStatus;

[ApiController]
[Authorize(Roles = "Tutor")]
[Route("api/tutors/tutoring-agreements")]
public sealed class UpdateTutoringAgreementStatusController(
    ISender sender,
    ILogger<UpdateTutoringAgreementStatusController> logger)
    : ControllerBase
{
    [HttpPut("{tutoringAgreementId:guid}/status")]
    public async Task<IActionResult> UpdateTutoringAgreementStatus(
        Guid tutoringAgreementId,
        [FromBody] UpdateTutoringAgreementStatusRequest request,
        CancellationToken cancellationToken)
    {
        var subject = User.FindFirst("sub")?.Value;
        var metadata = new RequestMetadata(
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: Request.Headers.UserAgent.ToString(),
            TraceId: HttpContext.TraceIdentifier);

        if (!Guid.TryParse(subject, out var userAccountId))
        {
            return Unauthorized();
        }

        logger.LogInformation(
            "Tutoring agreement status change requested. UserId: {UserId}, TutoringAgreementId: {TutoringAgreementId}, IsActive: {IsActive}, RequestMetadata: {RequestMetadata}",
            userAccountId,
            tutoringAgreementId,
            request.IsActive,
            metadata);

        await sender.Send(
            new UpdateTutoringAgreementStatusCommand(
                new UserAccountId(userAccountId),
                tutoringAgreementId,
                request.IsActive,
                metadata),
            cancellationToken);

        return NoContent();
    }
}
