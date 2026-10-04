using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.GetTutoringAgreementBalance;

public sealed record GetTutoringAgreementBalanceQuery(
    UserAccountId UserAccountId,
    Guid TutoringAgreementId,
    RequestMetadata RequestMetadata)
    : IRequest<GetTutoringAgreementBalanceResponse>;
