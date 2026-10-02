using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.RecordPayment;

[ApiController]
[Authorize(Roles = "Tutor")]
[Route("api/tutors/tutoring-agreements")]
public sealed class RecordPaymentController(
    ISender sender,
    ILogger<RecordPaymentController> logger)
    : ControllerBase
{
    [HttpPost("{tutoringAgreementId:guid}/payments")]
    public async Task<ActionResult<RecordPaymentResponse>> RecordPayment(
        Guid tutoringAgreementId,
        [FromBody] RecordPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var subject = User.FindFirst("sub")?.Value;
        var metadata = new RequestMetadata(
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: Request.Headers.UserAgent.ToString(),
            TraceId: HttpContext.TraceIdentifier);

        if (!Guid.TryParse(subject, out var userAccountId))
        {
            return Unauthorized();
        }

        logger.LogInformation(
            "Payment recording requested. UserId: {UserId}, TutoringAgreementId: {TutoringAgreementId}, Amount: {Amount}, PaidAtUtc: {PaidAtUtc}, RequestMetadata: {RequestMetadata}",
            userAccountId,
            tutoringAgreementId,
            request.Amount,
            request.PaidAtUtc,
            metadata);

        var result = await sender.Send(
            new RecordPaymentCommand(
                new UserAccountId(userAccountId),
                tutoringAgreementId,
                request.Amount,
                request.PaidAtUtc,
                request.Reference,
                metadata),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            result);
    }
}
