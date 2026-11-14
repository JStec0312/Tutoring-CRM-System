namespace Tutoring.Api.Features.Tutors.GetLessonCharges;

public sealed record LessonChargeResponse(
    Guid ChargeId,
    Guid AgreementId,
    Guid LessonId,
    string StudentName,
    string Subject,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    decimal Amount,
    string Currency,
    DateTimeOffset? PaidAtUtc);
