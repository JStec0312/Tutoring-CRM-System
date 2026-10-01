using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.UpdateTutoringAgreementHourlyRate;

public sealed record UpdateTutoringAgreementHourlyRateCommand(
    UserAccountId UserAccountId,
    Guid TutoringAgreementId,
    decimal? HourlyRate,
    RequestMetadata RequestMetadata)
    : IRequest;
