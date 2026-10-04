using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Domain.Billing;

public sealed class ChargeCannotBePaidException : DomainException
{
    public ChargeCannotBePaidException(Guid chargeId, string reason)
        : base(
            "Billing.ChargeCannotBePaid",
            $"Lesson charge '{chargeId}' cannot be paid because {reason}.")
    {
    }
}
