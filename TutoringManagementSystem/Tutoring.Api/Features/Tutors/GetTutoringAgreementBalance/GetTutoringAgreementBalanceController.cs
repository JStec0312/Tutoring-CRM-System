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
    public async Task<ActionResult<GetTutoringAgreementBalanceResponse>> GetBalance(
        Guid tutoringAgreementId,
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
            "Tutoring agreement balance requested. UserAccountId: {UserAccountId}, TutoringAgreementId: {TutoringAgreementId}, RequestMetadata: {RequestMetadata}",
            userAccountId,
            tutoringAgreementId,
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
