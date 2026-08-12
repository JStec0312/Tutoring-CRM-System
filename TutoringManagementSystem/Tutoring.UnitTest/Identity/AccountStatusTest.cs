using Tutoring.Domain.Identity;

namespace Tutoring.UnitTest.Identity;

public class AccountStatusTest
{
    [Theory]
    [InlineData(AccountStatus.PendingActivation, 0)]
    [InlineData(AccountStatus.Active, 1)]
    [InlineData(AccountStatus.Suspended, 2)]
    [InlineData(AccountStatus.Deleted, 3)]
    public void AccountStatus_withKnownValue_ShouldHaveExpectedNumericValue(AccountStatus status, int expected)
    {
        Assert.Equal(expected, (int)status);
    }
}
