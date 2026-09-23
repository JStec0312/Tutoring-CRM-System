using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Domain.Lessons;

public sealed class CanNotFinalizeLessonBeforeEndException : DomainException
{
    public CanNotFinalizeLessonBeforeEndException()
        : base(
            "Lessons.CannotBeFinalizedBeforeEnd",
            "Lesson cannot be completed or marked as missed before its scheduled end time.")
    {
    }
}