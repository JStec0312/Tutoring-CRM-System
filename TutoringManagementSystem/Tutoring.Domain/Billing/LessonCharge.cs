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

    public bool IsPaid { get; private set; }
    public DateTimeOffset? PaidAtUtc { get; private set; }

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

    internal void MarkAsPaid(DateTimeOffset paidAtUtc)
    {
        if (Status != ChargeStatus.Active)
        {
            throw new ChargeCannotBePaidException(Id.Value, "only active charges can be paid");
        }

        if (IsPaid)
        {
            throw new ChargeCannotBePaidException(Id.Value, "the charge is already paid");
        }

        IsPaid = true;
        PaidAtUtc = paidAtUtc;
    }

    public void EnsureCanBeMarkedAsPaid()
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
