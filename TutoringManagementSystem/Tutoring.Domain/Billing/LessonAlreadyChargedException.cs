using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Domain.Billing;

public sealed class LessonAlreadyChargedException: DomainException
{
    public LessonAlreadyChargedException() : base("Billing.LessonAlreadyCharged", "The lesson has already been charged.")
    {
    }
}