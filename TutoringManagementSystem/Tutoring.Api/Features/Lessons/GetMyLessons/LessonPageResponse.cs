namespace Tutoring.Api.Features.Lessons.GetMyLessons;

public sealed record LessonPageResponse(IReadOnlyList<LessonListItemResponse> Items, int Page, int PageSize, int TotalCount);

public sealed record LessonListItemResponse(Guid LessonId, Guid TutoringAgreementId, string Subject,
    string? PlannedTopic, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, string Status,
    string? CancellationReason, string StudentDisplayName, string? TutorFirstName, string? TutorLastName,
    IReadOnlyList<LessonListChargeResponse> Charges);

public sealed record LessonListChargeResponse(Guid ChargeId, Guid LessonId, decimal Amount,
    string Currency, DateTimeOffset? PaidAtUtc);
