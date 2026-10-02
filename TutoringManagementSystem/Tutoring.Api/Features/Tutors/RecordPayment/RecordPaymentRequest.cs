namespace Tutoring.Api.Features.Tutors.RecordPayment;

public sealed record RecordPaymentRequest(
    decimal Amount,
    DateTimeOffset PaidAtUtc,
    string? Reference);
