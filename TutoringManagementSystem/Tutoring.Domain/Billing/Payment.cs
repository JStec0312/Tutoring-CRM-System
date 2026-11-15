using Tutoring.Domain.Common;

namespace Tutoring.Domain.Billing;

public sealed class Payment
    : Entity<PaymentId>
{
    private Payment()
    {
    }

    public Payment(
        BillingAccountId billingAccountId,
        Money amount,
        DateTimeOffset paidAtUtc,
        PaymentReference? reference)
    {
        BillingAccountId = billingAccountId;
        ArgumentNullException.ThrowIfNull(amount);
        Amount = new Money(amount.Amount, amount.Currency);
        PaidAtUtc = paidAtUtc;
        Reference = reference;
    }

    public BillingAccountId BillingAccountId { get; private set; }

    public Money Amount { get; private set; } = null!;

    public DateTimeOffset PaidAtUtc { get; private set; }

    public PaymentReference? Reference { get; private set; }

    internal void SetAllocatedAmount(decimal amount)
    {
        Amount.SetAmount(amount);
    }
}
