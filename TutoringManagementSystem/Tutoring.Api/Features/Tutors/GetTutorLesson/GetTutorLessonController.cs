using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Domain.Identity;
using Tutoring.Domain.Lessons;

namespace Tutoring.Api.Features.Tutors.GetTutorLesson;

[ApiController]
[Authorize(Roles = nameof(UserRole.Tutor))]
[Route("api/tutors/me/lessons")]
public sealed class GetTutorLessonController(ISender sender) : ControllerBase
{
    [HttpGet("{lessonId:guid}")]
    public async Task<ActionResult<TutorLessonResponse>> Get(Guid lessonId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
            return Unauthorized();

        return Ok(await sender.Send(
            new GetTutorLessonQuery(new UserAccountId(accountId), new LessonId(lessonId)), cancellationToken));
    }
}
