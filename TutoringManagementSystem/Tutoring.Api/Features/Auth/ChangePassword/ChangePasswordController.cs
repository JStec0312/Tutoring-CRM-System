using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Auth.ChangePassword;

[ApiController]
[Authorize]
[Route("api/users/me/password")]
public sealed class ChangePasswordController(ISender sender) : ControllerBase
{
    [HttpPut]
    public async Task<IActionResult> Update(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId == Guid.Empty)
            return Unauthorized();

        await sender.Send(new ChangePasswordCommand(
            new UserAccountId(accountId), request.CurrentPassword, request.NewPassword), cancellationToken);
        return NoContent();
    }
}
