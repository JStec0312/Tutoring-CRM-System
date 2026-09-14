using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.GetAssignedStudents;

[ApiController]
[Authorize(Roles = "Tutor")]
[Route("api/tutors/me/students")]
public sealed class GetAssignedStudentsController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AssignedStudentResponse>>>
        GetAssignedStudents(
            CancellationToken cancellationToken)
    {
        var subject = User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(subject, out var userAccountId))
        {
            return Unauthorized();
        }

        var students = await sender.Send(
            new GetAssignedStudentsQuery(
                new UserAccountId(userAccountId)),
            cancellationToken);

        return Ok(students);
    }
}