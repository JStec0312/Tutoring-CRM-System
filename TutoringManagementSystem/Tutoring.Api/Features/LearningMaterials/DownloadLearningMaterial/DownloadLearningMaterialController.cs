using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.LearningMaterials.DownloadLearningMaterial;

[ApiController]
[Authorize(Roles = "Tutor,Student")]
[Route("api/learning-materials")]
public sealed class DownloadLearningMaterialController(ISender sender) : ControllerBase
{
    [HttpGet("{materialId:guid}/download")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Download(Guid materialId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
            return Unauthorized();

        var metadata = new RequestMetadata(HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(), HttpContext.TraceIdentifier);
        var download = await sender.Send(new DownloadLearningMaterialQuery(
            new UserAccountId(accountId), materialId, metadata), cancellationToken);

        return File(download.Content, download.ContentType, download.FileName);
    }
}
