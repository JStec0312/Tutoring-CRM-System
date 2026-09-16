using Tutoring.Domain.Billing;
using Tutoring.Domain.Common;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Domain.Tutors;

namespace Tutoring.UnitTest.TutoringAgreements;

public sealed class TutoringAgreementTest
{
    [Fact]
    public void UpdateStudentDetails_ShouldUpdateAgreementOwnedStudentData()
    {
        var agreement = new TutoringAgreement(
            new TutorId(Guid.NewGuid()),
            new StudentId(Guid.NewGuid()),
            new Subject("Mathematics"),
            null,
            new AgreementTitle("Tutoring"),
            DateTimeOffset.UtcNow);

        agreement.UpdateStudentDetails(
            new Subject("Physics"),
            new HourlyRate(new Money(80, new Currency("PLN"))),
            new EmailAddress("parent@example.com"),
            new PhoneNumber("500600700"),
            "Notes");

        Assert.Equal("Physics", agreement.Subject.Name);
        Assert.Equal(80, agreement.HourlyRate!.PricePerHour.Amount);
        Assert.Equal("parent@example.com", agreement.ContactEmail!.Value);
        Assert.Equal("500600700", agreement.ContactPhoneNumber!.Value);
        Assert.Equal("Notes", agreement.PrivateNotes);
    }

    [Fact]
    public void Money_ShouldRejectNegativeHourlyRate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Money(-1, new Currency("PLN")));
    }
}
