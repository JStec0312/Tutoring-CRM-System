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
        Assert.Equal("Lesson charge", charge.Description);
        Assert.Single(account.Charges);
    }

    [Fact]
    public void AddLessonCharge_ForSameLesson_ShouldThrow()
    {
        var account = new BillingAccount(
            new TutoringAgreementId(Guid.NewGuid()),
            DateTimeOffset.UtcNow);
        var lessonId = new LessonId(Guid.NewGuid());
        var amount = new Money(50, new Currency("PLN"));

        account.AddLessonCharge(lessonId, amount, DateTimeOffset.UtcNow);

        Assert.Throws<LessonAlreadyChargedException>(
            () => account.AddLessonCharge(
                lessonId,
                amount,
                DateTimeOffset.UtcNow));
    }

}
