using Tutoring.Domain.Common;
using Tutoring.Domain.Identity;

namespace Tutoring.UnitTest.Identity;

public class UserAccountTest
{
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 8, 21, 18, 0, 0, TimeSpan.Zero);

    private static UserAccount CreateUserAccount()
    {
        var email = new EmailAddress("test@example.com");
        var passwordHash = new PasswordHash("hashed-value");
        var profile = new PersonalProfile("john.doe", "John", "Doe", null);

        return new UserAccount(email, passwordHash, profile, CreatedAtUtc);
    }

    [Fact]
    public void Create_withValidData_ShouldSucceed()
    {
        var email = new EmailAddress("test@example.com");
        var passwordHash = new PasswordHash("hashed-value");
        var profile = new PersonalProfile("john.doe", "John", "Doe", null);

        var userAccount = new UserAccount(email, passwordHash, profile, CreatedAtUtc);

        Assert.Equal(email, userAccount.Email);
        Assert.Equal(passwordHash, userAccount.PasswordHash);
        Assert.Equal(profile, userAccount.Profile);
        Assert.Equal(AccountStatus.Active, userAccount.Status);
        Assert.Empty(userAccount.Roles);
        Assert.Empty(userAccount.RoleAssignments);
        Assert.NotEqual(Guid.Empty, userAccount.Id.Value);
        Assert.Equal(CreatedAtUtc, userAccount.CreatedAtUtc);
    }

    [Fact]
    public void AssignRole_withNewRole_ShouldAddRole()
    {
        var userAccount = CreateUserAccount();

        userAccount.AssignRole(UserRole.Tutor);

        Assert.Single(userAccount.Roles);
        Assert.Contains(UserRole.Tutor, userAccount.Roles);
    }

    [Fact]
    public void AssignRole_withDuplicateRole_ShouldNotAddDuplicate()
    {
        var userAccount = CreateUserAccount();

        userAccount.AssignRole(UserRole.Tutor);
        userAccount.AssignRole(UserRole.Tutor);

        Assert.Single(userAccount.Roles);
    }

    [Fact]
    public void AssignRole_withMultipleDifferentRoles_ShouldAddAllRoles()
    {
        var userAccount = CreateUserAccount();

        userAccount.AssignRole(UserRole.Tutor);
        userAccount.AssignRole(UserRole.Student);

        Assert.Equal(2, userAccount.Roles.Count);
        Assert.Contains(UserRole.Tutor, userAccount.Roles);
        Assert.Contains(UserRole.Student, userAccount.Roles);
    }
}
