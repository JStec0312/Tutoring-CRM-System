using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Students.GetStudentLessons;

[ApiController]
[Authorize(Roles = nameof(UserRole.Student))]
[Route("api/students/me")]
public sealed class GetStudentLessonsController(ISender sender) : ControllerBase
{
    [HttpGet("agreements/{agreementId:guid}/lessons")]
    public async Task<ActionResult<IReadOnlyList<StudentLessonResponse>>> Get(Guid agreementId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
        {
            return Unauthorized();
        }

        return Ok(await sender.Send(new GetStudentLessonsQuery(new UserAccountId(accountId), agreementId), cancellationToken));
    }
}
