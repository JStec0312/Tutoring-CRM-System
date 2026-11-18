using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Students.GetStudentLesson;

[ApiController]
[Authorize(Roles = nameof(UserRole.Student))]
[Route("api/students/me")]
public sealed class GetStudentLessonController(ISender sender) : ControllerBase
{
    [HttpGet("lessons/{lessonId:guid}")]
    public async Task<ActionResult<StudentLessonDetailsResponse>> Get(Guid lessonId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
        {
            return Unauthorized();
        }

        return Ok(await sender.Send(new GetStudentLessonQuery(new UserAccountId(accountId), lessonId), cancellationToken));
    }
}
