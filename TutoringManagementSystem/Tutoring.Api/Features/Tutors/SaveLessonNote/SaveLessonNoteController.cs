using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.SaveLessonNote;

[ApiController]
[Authorize(Roles = nameof(UserRole.Tutor))]
[Route("api/tutors/me/lessons")]
public sealed class SaveLessonNoteController(ISender sender) : ControllerBase
{
    [HttpPut("{lessonId:guid}/note")]
    public async Task<IActionResult> Save(Guid lessonId, SaveLessonNoteRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
            return Unauthorized();

        var metadata = new RequestMetadata(HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(), HttpContext.TraceIdentifier);
        await sender.Send(new SaveLessonNoteCommand(new UserAccountId(accountId), lessonId,
            request.Topic, request.CoveredTopics, request.StudentProgress, metadata), cancellationToken);
        return NoContent();
    }
}
