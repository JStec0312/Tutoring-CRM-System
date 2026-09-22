using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Domain.Lessons;

public sealed class CanNotRescheduleInactiveLessonException : DomainException
{
    public CanNotRescheduleInactiveLessonException() : base("Lessons.CannotBeRescheduled", "Only scheduled lessons can be rescheduled.")
    {
    }
}
