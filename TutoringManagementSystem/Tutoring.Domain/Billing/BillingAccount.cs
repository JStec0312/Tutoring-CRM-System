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

    public BillingAccount(
        TutoringAgreementId tutoringAgreementId,
        DateTimeOffset createdAtUtc)
    {
        TutoringAgreementId = tutoringAgreementId;
        Status = BillingAccountStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    public TutoringAgreementId TutoringAgreementId { get; private set; }

    public BillingAccountStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<LessonCharge> Charges =>
        _charges.AsReadOnly();

    public IReadOnlyCollection<Payment> Payments =>
        _payments.AsReadOnly();

    public LessonCharge AddLessonCharge(
        LessonId lessonId,
        Money amount,
        DateTimeOffset chargedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(amount);

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

    public Payment MarkChargeAsPaid(
        LessonChargeId chargeId,
        DateTimeOffset paidAtUtc,
        PaymentReference? reference)
    {
        return MarkChargesAsPaid(
            [chargeId],
            paidAtUtc,
            reference);
    }

    public Payment MarkChargesAsPaid(
        IReadOnlyCollection<LessonChargeId> chargeIds,
        DateTimeOffset paidAtUtc,
        PaymentReference? reference)
    {
        ArgumentNullException.ThrowIfNull(chargeIds);

        if (chargeIds.Count == 0)
        {
            throw new ArgumentException(
                "At least one lesson charge is required.",
                nameof(chargeIds));
        }

        if (chargeIds.Distinct().Count() != chargeIds.Count)
        {
            throw new ArgumentException(
                "Lesson charge IDs must be unique.",
                nameof(chargeIds));
        }

        var charges = FindCharges(chargeIds);

        foreach (var charge in charges)
        {
            charge.EnsureCanBeMarkedAsPaid();
        }

        if (charges.Any(charge => charge.BillingAccountId != Id))
        {
            throw new InvalidOperationException(
                "All lesson charges must belong to the same billing account.");
        }

        var currency = charges[0].Amount.Currency;

        if (charges.Any(
                charge => charge.Amount.Currency.Code != currency.Code))
        {
            throw new InvalidOperationException(
                "All lesson charges must use the same currency.");
        }

        var totalAmount = charges.Sum(
            charge => charge.Amount.Amount);

        var payment = new Payment(
            Id,
            new Money(totalAmount, currency),
            paidAtUtc,
            reference);

        _payments.Add(payment);

        foreach (var charge in charges)
        {
            charge.MarkAsPaid(payment);
        }

        return payment;
    }

    private LessonCharge[] FindCharges(
        IReadOnlyCollection<LessonChargeId> chargeIds)
    {
        var charges = _charges
            .Where(charge => chargeIds.Contains(charge.Id))
            .ToArray();

        if (charges.Length != chargeIds.Count)
        {
            var missingChargeId = chargeIds
                .Except(charges.Select(charge => charge.Id))
                .First();

            throw new ChargeNotFoundInBillingAccountException(
                missingChargeId.Value);
        }

        return charges;
    }
}