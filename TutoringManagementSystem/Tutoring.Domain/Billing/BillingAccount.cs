using Tutoring.Domain.Common;
using Tutoring.Domain.TutoringAgreements;

namespace Tutoring.Domain.Billing;

public sealed class BillingAccount
    : Entity<BillingAccountId>
{
    private readonly List<LessonCharge> _charges = new();
    private readonly List<Payment> _payments = new();

    private BillingAccount()
    {
    }

    public TutoringAgreementId TutoringAgreementId { get; private set; }

    public TutoringAgreement Agreement { get; private set; } = null!;

    public BillingAccountStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<LessonCharge> Charges => _charges.AsReadOnly();

    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();
}
