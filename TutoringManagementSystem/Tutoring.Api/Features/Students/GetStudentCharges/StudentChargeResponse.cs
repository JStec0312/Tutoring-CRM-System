namespace Tutoring.Api.Features.Students.GetStudentCharges;

public sealed record StudentChargeResponse(Guid ChargeId, Guid LessonId, DateTimeOffset StartsAtUtc,
    decimal Amount, string Currency, DateTimeOffset? PaidAtUtc);
