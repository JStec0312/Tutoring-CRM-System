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
    public Lesson Lesson { get; private set; } = null!;
    public Money Amount { get; private set; } = null!;

    public DateTimeOffset ChargedAtUtc { get; private set; }

    public ChargeStatus Status { get; private set; }

    public string Description { get; private set; } = null!;
}
