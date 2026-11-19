using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Lessons.GetMyLessons;

[ApiController]
[Authorize(Roles = nameof(UserRole.Tutor) + "," + nameof(UserRole.Student))]
[Route("api/lessons")]
public sealed class GetMyLessonsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<LessonPageResponse>> Get(
        [FromQuery] GetMyLessonsRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
        {
            return Unauthorized();
        }

        if (request.TutorOnly)
        {
            if (!User.IsInRole(nameof(UserRole.Tutor))) return Forbid();
        }
        else
        {
            if (!User.IsInRole(nameof(UserRole.Student))) return Forbid();
        }

        var metadata = new RequestMetadata(HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(), HttpContext.TraceIdentifier);
        return Ok(await sender.Send(new GetMyLessonsQuery(new UserAccountId(accountId),
            request.TutorOnly, request.Page, request.PageSize, request.View, request.AgreementId, metadata), cancellationToken));
    }
}
