using Tutoring.Domain.Identity;

namespace Tutoring.UnitTest.Identity;

public class UserAccountRoleTest
{
    [Theory]
    [InlineData(UserRole.Tutor)]
    [InlineData(UserRole.Student)]
    [InlineData(UserRole.Administrator)]
    public void Create_withValidData_ShouldSucceed(UserRole role)
    {
        var userAccountId = UserAccountId.New();

        var roleAssignment = new UserAccountRole(userAccountId, role);

        Assert.Equal(userAccountId, roleAssignment.UserAccountId);
        Assert.Equal(role, roleAssignment.Role);
    }
}
