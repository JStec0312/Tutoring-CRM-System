using Tutoring.Domain.Identity;

namespace Tutoring.UnitTest.Identity;

public class UserRoleTest
{
    [Theory]
    [InlineData(UserRole.Tutor, 0)]
    [InlineData(UserRole.Student, 1)]
    [InlineData(UserRole.Administrator, 2)]
    public void UserRole_withKnownValue_ShouldHaveExpectedNumericValue(UserRole role, int expected)
    {
        Assert.Equal(expected, (int)role);
    }
}
