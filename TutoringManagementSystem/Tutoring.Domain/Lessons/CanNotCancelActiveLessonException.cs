using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Domain.Lessons;

public sealed class CanNotCancelActiveLessonException : DomainException
{
    public CanNotCancelActiveLessonException() : base("Lesson.CancelActive", "Cannot cancel an active lesson.")
    {
    }
}