using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Domain.Lessons;

public sealed class CanNotFinalizeLessonException : DomainException
{
    public CanNotFinalizeLessonException(LessonStatus currentStatus)
        : base(
            "Lessons.CannotBeFinalized",
            $"Lesson with status '{currentStatus}' cannot be finalized.")
    {
    }
}