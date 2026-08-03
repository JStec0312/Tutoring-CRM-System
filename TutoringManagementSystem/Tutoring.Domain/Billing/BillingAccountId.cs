using Tutoring.Domain.Common;

namespace Tutoring.Domain.Billing;

public readonly record struct BillingAccountId(Guid Value)
    : IDomainId
{
    public static BillingAccountId New() => new(Guid.NewGuid());
}
