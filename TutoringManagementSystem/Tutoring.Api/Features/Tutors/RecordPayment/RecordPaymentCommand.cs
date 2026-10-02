using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.RecordPayment;

public sealed record RecordPaymentCommand(
    UserAccountId UserAccountId,
    Guid TutoringAgreementId,
    decimal Amount,
    DateTimeOffset PaidAtUtc,
    string? Reference,
    RequestMetadata RequestMetadata)
    : IRequest<RecordPaymentResponse>;
