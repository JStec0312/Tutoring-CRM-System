using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Students.GetStudentAgreements;

[ApiController]
[Authorize(Roles = nameof(UserRole.Student))]
[Route("api/students/me")]
public sealed class GetStudentAgreementsController(ISender sender) : ControllerBase
{
    [HttpGet("agreements")]
    public async Task<ActionResult<IReadOnlyList<StudentAgreementResponse>>> Get(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
        {
            return Unauthorized();
        }

        return Ok(await sender.Send(new GetStudentAgreementsQuery(new UserAccountId(accountId)), cancellationToken));
    }
}
