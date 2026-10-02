using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Domain.Billing;

public sealed class PaymentMustBePositiveException : DomainException
{
    public PaymentMustBePositiveException()
        : base("Billing.PaymentNotPositive","Payment amount must be positive.")
    {
    }
}