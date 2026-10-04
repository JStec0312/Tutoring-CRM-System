using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.GetTutoringAgreementBalance;

[ApiController]
[Authorize(Roles = "Tutor")]
[Route("api/tutors/tutoring-agreements")]
public sealed class GetTutoringAgreementBalanceController(
    ISender sender,
    ILogger<GetTutoringAgreementBalanceController> logger)
    : ControllerBase
{
    [HttpGet("{tutoringAgreementId:guid}/balance")]
    public async Task<ActionResult<GetTutoringAgreementBalanceResponse>>
        GetTutoringAgreementBalance(
            Guid tutoringAgreementId,
            CancellationToken cancellationToken)
    {
        var subject = User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(subject, out var userAccountId))
        {
            return Unauthorized();
        }

        var metadata = new RequestMetadata(
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: Request.Headers.UserAgent.ToString(),
            TraceId: HttpContext.TraceIdentifier);

        logger.LogInformation(
            "Tutoring agreement balance requested. TutoringAgreementId: {TutoringAgreementId}, UserAccountId: {UserAccountId}, RequestMetadata: {RequestMetadata}",
            tutoringAgreementId,
            userAccountId,
            metadata);

        var balance = await sender.Send(
            new GetTutoringAgreementBalanceQuery(
                new UserAccountId(userAccountId),
                tutoringAgreementId,
                metadata),
            cancellationToken);

        return Ok(balance);
    }
}
