namespace Tutoring.Api.Features.Tutors.RecordPayment;

public sealed record RecordPaymentResponse(
    Guid PaymentId,
    decimal Amount,
    string Currency,
    DateTimeOffset PaidAtUtc,
    string? Reference);
