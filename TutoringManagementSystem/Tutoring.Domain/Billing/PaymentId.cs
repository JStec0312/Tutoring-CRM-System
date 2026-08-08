using Tutoring.Domain.Common;

namespace Tutoring.Domain.Billing;

public readonly record struct PaymentId(Guid Value)
    : DomainId<PaymentId>
{
    public static PaymentId New() => new(Guid.NewGuid());
}
