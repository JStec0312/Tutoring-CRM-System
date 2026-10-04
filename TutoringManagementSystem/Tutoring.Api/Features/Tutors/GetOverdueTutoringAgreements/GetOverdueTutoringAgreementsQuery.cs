using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.GetOverdueTutoringAgreements;

public sealed record GetOverdueTutoringAgreementsQuery(
    UserAccountId UserAccountId,
    RequestMetadata RequestMetadata)
    : IRequest<IReadOnlyCollection<OverdueTutoringAgreementResponse>>;
