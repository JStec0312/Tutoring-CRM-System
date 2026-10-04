namespace Tutoring.Api.Features.Tutors.MarkLessonChargePaid;

public sealed record MarkLessonChargePaidRequest(
    DateTimeOffset PaidAtUtc,
    string? Reference);
