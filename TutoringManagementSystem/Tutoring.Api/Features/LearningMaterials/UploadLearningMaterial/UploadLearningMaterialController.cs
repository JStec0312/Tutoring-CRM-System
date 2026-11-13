using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.LearningMaterials.UploadLearningMaterial;

[ApiController]
[Authorize(Roles = nameof(UserRole.Tutor) + "," + nameof(UserRole.Student))]
[Route("api/learning-materials")]
public sealed class UploadLearningMaterialController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [TypeFilter(typeof(UploadRequestLimitsFilter))]
    public async Task<ActionResult<UploadLearningMaterialResponse>> Upload(
        [FromForm] UploadLearningMaterialRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
            return Unauthorized();

        var metadata = new RequestMetadata(HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(), HttpContext.TraceIdentifier);
        await using var stream = request.File.OpenReadStream();
        var response = await sender.Send(new UploadLearningMaterialCommand(
            new UserAccountId(accountId), request.TutoringAgreementId!.Value, request.LessonId,
            request.Title, stream, request.File.FileName, request.File.Length, metadata), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
