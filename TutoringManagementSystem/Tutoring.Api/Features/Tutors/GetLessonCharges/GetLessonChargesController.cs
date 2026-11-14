using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.GetLessonCharges;

[ApiController]
[Authorize(Roles = nameof(UserRole.Tutor))]
[Route("api/tutors/me/lesson-charges")]
public sealed class GetLessonChargesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LessonChargeResponse>>> Get(
        Guid? agreementId, Guid? lessonId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
            return Unauthorized();

        return Ok(await sender.Send(new GetLessonChargesQuery(
            new UserAccountId(accountId), agreementId, lessonId), cancellationToken));
    }
}
