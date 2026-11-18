using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Students.GetStudentCharges;

[ApiController]
[Authorize(Roles = nameof(UserRole.Student))]
[Route("api/students/me")]
public sealed class GetStudentChargesController(ISender sender) : ControllerBase
{
    [HttpGet("agreements/{agreementId:guid}/charges")]
    public async Task<ActionResult<IReadOnlyList<StudentChargeResponse>>> Get(Guid agreementId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
        {
            return Unauthorized();
        }

        return Ok(await sender.Send(new GetStudentChargesQuery(new UserAccountId(accountId), agreementId), cancellationToken));
    }
}
