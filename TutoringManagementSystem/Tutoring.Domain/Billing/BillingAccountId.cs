using Tutoring.Domain.Common;

namespace Tutoring.Domain.Billing;

public readonly record struct BillingAccountId(Guid Value)
    : DomainId<BillingAccountId>
{
    public static BillingAccountId New() => new(Guid.NewGuid());
}
