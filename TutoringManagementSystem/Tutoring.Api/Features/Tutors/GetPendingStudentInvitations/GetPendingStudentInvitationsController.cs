using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.GetPendingStudentInvitations;

[ApiController]
[Authorize(Roles = nameof(UserRole.Tutor))]
[Route("api/tutors/me/student-invitations")]
public sealed class GetPendingStudentInvitationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PendingStudentInvitationResponse>>> Get(
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
            return Unauthorized();

        return Ok(await sender.Send(
            new GetPendingStudentInvitationsQuery(new UserAccountId(accountId)), cancellationToken));
    }
}
