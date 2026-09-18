using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Tutors.Exceptions;

public sealed class LessonTimeConflictException(
    DateTimeOffset startsAtUtc,
    DateTimeOffset endsAtUtc)
    : ConflictException(
        "Lessons.TimeConflict",
        $"Lesson time slot {startsAtUtc:u} - {endsAtUtc:u} conflicts with another scheduled lesson.")
{
}
