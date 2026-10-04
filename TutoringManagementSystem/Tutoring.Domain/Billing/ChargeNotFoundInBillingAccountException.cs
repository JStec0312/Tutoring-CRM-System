using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Domain.Billing;

public sealed class ChargeNotFoundInBillingAccountException : DomainException
{
    public ChargeNotFoundInBillingAccountException(Guid chargeId)
        : base("Billing.ChargeNotFound", $"Lesson charge '{chargeId}' was not found in the billing account.")
    {
    }
}
