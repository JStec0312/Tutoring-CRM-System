using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.GetOverdueTutoringAgreements;

[ApiController]
[Authorize(Roles = "Tutor")]
[Route("api/tutors/tutoring-agreements")]
public sealed class GetOverdueTutoringAgreementsController(
    ISender sender,
    ILogger<GetOverdueTutoringAgreementsController> logger)
    : ControllerBase
{
    [HttpGet("overdue")]
    public async Task<ActionResult<IReadOnlyCollection<OverdueTutoringAgreementResponse>>>
        GetOverdueTutoringAgreements(
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
            "Overdue tutoring agreements requested. UserAccountId: {UserAccountId}, RequestMetadata: {RequestMetadata}",
            userAccountId,
            metadata);

        var agreements = await sender.Send(
            new GetOverdueTutoringAgreementsQuery(
                new UserAccountId(userAccountId),
                metadata),
            cancellationToken);

        return Ok(agreements);
    }
}
