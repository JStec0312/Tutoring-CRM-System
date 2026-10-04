using Tutoring.Domain.Billing;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.TutoringAgreements;

namespace Tutoring.UnitTest.Billing;

public sealed class BillingAccountTest
{
    [Fact]
    public void AddLessonCharge_ShouldCreateChargeWithExpectedProperties()
    {
        var agreementId = new TutoringAgreementId(Guid.NewGuid());
        var lessonId = new LessonId(Guid.NewGuid());
        var chargedAt = DateTimeOffset.UtcNow;
        var amount = new Money(75.50m, new Currency("PLN"));
        var account = new BillingAccount(agreementId, chargedAt);

        var charge = account.AddLessonCharge(lessonId, amount, chargedAt);

        Assert.Equal(account.Id, charge.BillingAccountId);
        Assert.Equal(lessonId, charge.LessonId);
        Assert.Equal(amount, charge.Amount);
        Assert.Equal(chargedAt, charge.ChargedAtUtc);
        Assert.Equal(ChargeStatus.Active, charge.Status);
        Assert.False(charge.IsPaid);
        Assert.Null(charge.PaidAtUtc);
        Assert.Equal("Lesson charge", charge.Description);
        Assert.Single(account.Charges);
    }

    [Fact]
    public void AddLessonCharge_ForSameLesson_ShouldThrow()
    {
        var account = CreateAccount();
        var lessonId = new LessonId(Guid.NewGuid());
        var amount = new Money(50, new Currency("PLN"));

        account.AddLessonCharge(lessonId, amount, DateTimeOffset.UtcNow);

        Assert.Throws<LessonAlreadyChargedException>(
            () => account.AddLessonCharge(lessonId, amount, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MarkChargeAsPaid_ShouldMarkChargeAndCreatePaymentFromChargeAmount()
    {
        var account = CreateAccount();
        var charge = account.AddLessonCharge(
            new LessonId(Guid.NewGuid()),
            new Money(75.50m, new Currency("PLN")),
            DateTimeOffset.UtcNow);
        var paidAt = DateTimeOffset.UtcNow.AddMinutes(5);
        var reference = new PaymentReference("TRANSFER-001");

        var payment = account.MarkChargeAsPaid(charge.Id, paidAt, reference);

        Assert.True(charge.IsPaid);
        Assert.Equal(paidAt, charge.PaidAtUtc);
        Assert.Equal(account.Id, payment.BillingAccountId);
        Assert.Equal(75.50m, payment.Amount.Amount);
        Assert.Equal("PLN", payment.Amount.Currency.Code);
        Assert.Equal(paidAt, payment.PaidAtUtc);
        Assert.Equal(reference, payment.Reference);
        Assert.Single(account.Payments);
    }

    [Fact]
    public void MarkChargesAsPaid_ShouldMarkAllChargesAndCreateOnePaymentForTheirTotal()
    {
        var account = CreateAccount();
        var first = AddCharge(account, 25m);
        var second = AddCharge(account, 50.50m);
        var paidAt = DateTimeOffset.UtcNow;

        var payment = account.MarkChargesAsPaid(
            [first.Id, second.Id],
            paidAt,
            null);

        Assert.All(account.Charges, charge => Assert.True(charge.IsPaid));
        Assert.All(account.Charges, charge => Assert.Equal(paidAt, charge.PaidAtUtc));
        Assert.Equal(75.50m, payment.Amount.Amount);
        Assert.Single(account.Payments);
    }

    [Fact]
    public void MarkChargesAsPaid_WhenOneChargeCannotBePaid_ShouldNotChangeAnyChargeOrCreatePayment()
    {
        var account = CreateAccount();
        var alreadyPaid = AddCharge(account, 25m);
        var unpaid = AddCharge(account, 50m);
        account.MarkChargeAsPaid(alreadyPaid.Id, DateTimeOffset.UtcNow, null);
        var paymentCount = account.Payments.Count;

        Assert.Throws<ChargeCannotBePaidException>(
            () => account.MarkChargesAsPaid(
                [alreadyPaid.Id, unpaid.Id],
                DateTimeOffset.UtcNow,
                null));

        Assert.True(alreadyPaid.IsPaid);
        Assert.False(unpaid.IsPaid);
        Assert.Equal(paymentCount, account.Payments.Count);
    }

    [Fact]
    public void MarkChargesAsPaid_WithDuplicateIds_ShouldThrowWithoutCreatingPayment()
    {
        var account = CreateAccount();
        var charge = AddCharge(account, 25m);

        Assert.Throws<ArgumentException>(
            () => account.MarkChargesAsPaid(
                [charge.Id, charge.Id],
                DateTimeOffset.UtcNow,
                null));

        Assert.False(charge.IsPaid);
        Assert.Empty(account.Payments);
    }

    private static BillingAccount CreateAccount() => new(
        new TutoringAgreementId(Guid.NewGuid()),
        DateTimeOffset.UtcNow);

    private static LessonCharge AddCharge(BillingAccount account, decimal amount) =>
        account.AddLessonCharge(
            new LessonId(Guid.NewGuid()),
            new Money(amount, new Currency("PLN")),
            DateTimeOffset.UtcNow);
}
