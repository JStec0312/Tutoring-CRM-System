using Tutoring.Domain.Common;
using Tutoring.Domain.Lessons;
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

    public BillingAccountStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<LessonCharge> Charges => _charges.AsReadOnly();

    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    public BillingAccount(TutoringAgreementId tutoringAgreementId, DateTimeOffset createdAtUtc)
    {
        TutoringAgreementId = tutoringAgreementId;
        Status = BillingAccountStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    public LessonCharge AddLessonCharge(LessonId lessonId, Money amount, DateTimeOffset chargedAtUtc)
    {
        if (_charges.Any(charge => charge.LessonId == lessonId))
        {
            throw new LessonAlreadyChargedException();
        }

        var charge = new LessonCharge(
            Id,
            lessonId,
            amount,
            chargedAtUtc);

        _charges.Add(charge);

        return charge;
    }
}
