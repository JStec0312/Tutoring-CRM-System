using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Domain.Identity;
using Tutoring.Domain.TutoringAgreements;

namespace Tutoring.Api.Features.LearningMaterials.GetLearningMaterials;

[ApiController]
[Authorize(Roles = nameof(UserRole.Tutor) + "," + nameof(UserRole.Student))]
[Route("api/learning-materials")]
public sealed class GetLearningMaterialsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LearningMaterialResponse>>> Get(
        [FromQuery] Guid tutoringAgreementId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
            return Unauthorized();

        return Ok(await sender.Send(new GetLearningMaterialsQuery(
            new UserAccountId(accountId), new TutoringAgreementId(tutoringAgreementId)), cancellationToken));
    }
}
