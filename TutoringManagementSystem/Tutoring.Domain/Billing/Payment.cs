using Tutoring.Domain.Common;

namespace Tutoring.Domain.Billing;

public sealed class Payment
    : Entity<PaymentId>
{
    private BillingAccountId id;

    public Payment(BillingAccountId id, Money amount, DateTimeOffset paidAtUtc, PaymentReference? reference)
    {
        this.id = id;
        Amount = amount;
        PaidAtUtc = paidAtUtc;
        Reference = reference;
    }

    private Payment()
    {
    }

    public BillingAccountId BillingAccountId { get; private set; }

    public Money Amount { get; private set; } = null!;

    public DateTimeOffset PaidAtUtc { get; private set; }

    public PaymentReference? Reference { get; private set; }
}
