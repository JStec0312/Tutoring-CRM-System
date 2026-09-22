using Tutoring.Domain.Lessons;

namespace Tutoring.Api.Features.Lessons.CancelLesson;

public sealed record CancelLessonRequest(
    LessonCancellationParty? CancellationParty,
    string? Reason);