using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Domain.Identity;
using Tutoring.Domain.TutoringAgreements;

namespace Tutoring.Api.Features.Tutors.GetTutoringAgreementOverview;

[ApiController]
[Authorize(Roles = nameof(UserRole.Tutor))]
[Route("api/tutors/tutoring-agreements")]
public sealed class GetTutoringAgreementOverviewController(ISender sender) : ControllerBase
{
    [HttpGet("{agreementId:guid}/overview")]
    public async Task<ActionResult<TutoringAgreementOverviewResponse>> Get(
        Guid agreementId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
            return Unauthorized();

        return Ok(await sender.Send(new GetTutoringAgreementOverviewQuery(
            new UserAccountId(accountId), new TutoringAgreementId(agreementId)), cancellationToken));
    }
}
