using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Domain.Identity;
using Tutoring.Domain.Students;

namespace Tutoring.Api.Features.Tutors.AddStudentAgreement;

[ApiController]
[Authorize(Roles = nameof(UserRole.Tutor))]
[Route("api/tutors/me/students")]
public sealed class AddStudentAgreementController(ISender sender) : ControllerBase
{
    [HttpPost("{studentId:guid}/agreements")]
    public async Task<IActionResult> Create(Guid studentId, AddStudentAgreementRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
            return Unauthorized();

        var agreementId = await sender.Send(new AddStudentAgreementCommand(
            new UserAccountId(accountId), new StudentId(studentId), request.Title, request.Subject, request.HourlyRate),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { agreementId });
    }
}
