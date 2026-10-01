using Tutoring.Domain.Billing;
using Tutoring.Domain.Common;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Domain.Tutors;

namespace Tutoring.UnitTest.TutoringAgreements;

public sealed class TutoringAgreementTest
{


    [Fact]
    public void Money_ShouldRejectNegativeHourlyRate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Money(-1, new Currency("PLN")));
    }
}
