namespace Tutoring.Api.Features.Tutors.MarkLessonChargesPaid;

public sealed record MarkLessonChargesPaidRequest(
    IReadOnlyCollection<Guid> LessonChargeIds,
    DateTimeOffset? PaidAtUtc,
    string? Reference);
