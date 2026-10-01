using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.UpdateTutoringAgreementHourlyRate;

[ApiController]
[Authorize(Roles = "Tutor")]
[Route("api/tutors/tutoring-agreements")]
public sealed class UpdateTutoringAgreementHourlyRateController(
    ISender sender,
    ILogger<UpdateTutoringAgreementHourlyRateController> logger)
    : ControllerBase
{
    [HttpPut("{tutoringAgreementId:guid}/hourly-rate")]
    public async Task<IActionResult> UpdateTutoringAgreementHourlyRate(
        Guid tutoringAgreementId,
        [FromBody] UpdateTutoringAgreementHourlyRateRequest request,
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
            "Tutoring agreement hourly rate update requested. UserId: {UserId}, TutoringAgreementId: {TutoringAgreementId}, RequestMetadata: {RequestMetadata}",
            userAccountId,
            tutoringAgreementId,
            metadata);

        await sender.Send(
            new UpdateTutoringAgreementHourlyRateCommand(
                new UserAccountId(userAccountId),
                tutoringAgreementId,
                request.HourlyRate,
                metadata),
            cancellationToken);

        return NoContent();
    }
}
