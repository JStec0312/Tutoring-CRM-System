using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.UpdateTutoringAgreementStatus;

public sealed record UpdateTutoringAgreementStatusCommand(
    UserAccountId UserAccountId,
    Guid TutoringAgreementId,
    bool IsActive,
    RequestMetadata RequestMetadata)
    : IRequest;
