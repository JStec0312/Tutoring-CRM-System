using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Domain.Lessons;

public sealed class CanNotCancelActiveLessonException : DomainException
{
    public CanNotCancelActiveLessonException() : base("Lessons.CanNotBeCancelled", "Cannot cancel an active lesson.")
    {
    }
}