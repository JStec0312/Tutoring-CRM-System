using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.UpdateTutoringAgreementDetails;

[ApiController]
[Authorize(Roles = "Tutor")]
[Route("api/tutors/tutoring-agreements")]
public sealed class UpdateTutoringAgreementDetailsController(
    ISender sender,
    ILogger<UpdateTutoringAgreementDetailsController> logger)
    : ControllerBase
{
    [HttpPut("{tutoringAgreementId:guid}/details")]
    public async Task<IActionResult> UpdateTutoringAgreementDetails(
        Guid tutoringAgreementId,
        [FromBody] UpdateTutoringAgreementDetailsRequest request,
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
            "Tutoring agreement details update requested. UserId: {UserId}, TutoringAgreementId: {TutoringAgreementId}, RequestMetadata: {RequestMetadata}",
            userAccountId,
            tutoringAgreementId,
            metadata);

        await sender.Send(
            new UpdateTutoringAgreementDetailsCommand(
                new UserAccountId(userAccountId),
                tutoringAgreementId,
                request.Subject,
                request.ContactEmail,
                request.ContactPhoneNumber,
                request.Notes,
                metadata),
            cancellationToken);

        return NoContent();
    }
}
