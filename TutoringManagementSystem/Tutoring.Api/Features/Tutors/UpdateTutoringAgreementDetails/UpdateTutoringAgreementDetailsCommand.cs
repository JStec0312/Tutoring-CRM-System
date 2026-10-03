using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.UpdateTutoringAgreementDetails;

public sealed record UpdateTutoringAgreementDetailsCommand(
    UserAccountId UserAccountId,
    Guid TutoringAgreementId,
    string? Subject,
    string? Notes,
    RequestMetadata RequestMetadata)
    : IRequest;
