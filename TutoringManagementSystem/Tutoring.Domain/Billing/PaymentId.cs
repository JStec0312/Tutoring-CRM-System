using Tutoring.Domain.Common;

namespace Tutoring.Domain.Billing;

public readonly record struct PaymentId(Guid Value)
    : IDomainId
{
    public static PaymentId New() => new(Guid.NewGuid());
}
