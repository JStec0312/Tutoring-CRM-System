using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.LearningMaterials.AttachLearningMaterialToLesson;

[ApiController]
[Authorize(Roles = "Tutor,Student")]
[Route("api/learning-materials")]
public sealed class AttachLearningMaterialToLessonController(ISender sender) : ControllerBase
{
    [HttpPut("{materialId:guid}/lesson")]
    public async Task<IActionResult> Attach(Guid materialId, [FromBody] AttachLearningMaterialToLessonRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
            return Unauthorized();

        var metadata = new RequestMetadata(HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(), HttpContext.TraceIdentifier);
        await sender.Send(new AttachLearningMaterialToLessonCommand(new UserAccountId(accountId), materialId,
            request.LessonId!.Value, metadata), cancellationToken);
        return NoContent();
    }
}
