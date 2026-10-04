using Tutoring.Domain.Common;
using Tutoring.Domain.Lessons;

namespace Tutoring.Domain.Billing;

public sealed class LessonCharge
    : Entity<LessonChargeId>
{
    private LessonCharge()
    {
    }

    public BillingAccountId BillingAccountId { get; private set; }

    public LessonId LessonId { get; private set; }
    public Money Amount { get; private set; } = null!;

    public DateTimeOffset ChargedAtUtc { get; private set; }

    public ChargeStatus Status { get; private set; }

    public string Description { get; private set; } = null!;

    public PaymentId? PaymentId { get; private set; }

    public Payment? Payment { get; private set; }

    public bool IsPaid => PaymentId is not null;

    internal LessonCharge(
        BillingAccountId billingAccountId,
        LessonId lessonId,
        Money amount,
        DateTimeOffset chargedAtUtc)
    {
        BillingAccountId = billingAccountId;
        LessonId = lessonId;
        Amount = amount;
        ChargedAtUtc = chargedAtUtc;
        Status = ChargeStatus.Active;
        Description = "Lesson charge";
    }

    internal void MarkAsPaid(Payment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);

        EnsureCanBeMarkedAsPaid();

        if (payment.BillingAccountId != BillingAccountId)
        {
            throw new ChargeCannotBePaidException(Id.Value, "the payment belongs to a different billing account");
        }

        if (payment.Amount.Currency.Code != Amount.Currency.Code)
        {
            throw new ChargeCannotBePaidException(Id.Value, "the payment uses a different currency");
        }

        Payment = payment;
        PaymentId = payment.Id;
    }

    internal void EnsureCanBeMarkedAsPaid()
    {
        if (Status != ChargeStatus.Active)
        {
            throw new ChargeCannotBePaidException(Id.Value, "only active charges can be paid");
        }

        if (IsPaid)
        {
            throw new ChargeCannotBePaidException(Id.Value, "the charge is already paid");
        }
    }
}
