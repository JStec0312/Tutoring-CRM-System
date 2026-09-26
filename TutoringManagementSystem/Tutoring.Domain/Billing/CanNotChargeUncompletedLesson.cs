using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Domain.Billing;

public sealed class CanNotChargeUncompletedLessonException: DomainException
{
    public CanNotChargeUncompletedLessonException() : base("Billing.CanNotChargeUncompletedLesson", "Cannot charge an uncompleted lesson.")
    {
    }
}