using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.LearningMaterials.DeleteLearningMaterial;

[ApiController]
[Authorize(Roles = "Tutor,Student")]
[Route("api/learning-materials")]
public sealed class DeleteLearningMaterialController(ISender sender) : ControllerBase
{
    [HttpDelete("{materialId:guid}")]
    public async Task<IActionResult> Delete(Guid materialId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
            return Unauthorized();

        var metadata = new RequestMetadata(HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(), HttpContext.TraceIdentifier);
        await sender.Send(new DeleteLearningMaterialCommand(
            new UserAccountId(accountId), materialId, User.IsInRole("Tutor"), metadata), cancellationToken);
        return NoContent();
    }
}
