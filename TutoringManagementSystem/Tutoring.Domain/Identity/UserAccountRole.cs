namespace Tutoring.Domain.Identity;

public sealed class UserAccountRole
{
    private UserAccountRole()
    {
    }
    public UserAccountRole(UserAccountId userAccountId, UserRole role)
    {
        UserAccountId = userAccountId;
        Role = role;
    }
    public UserAccountId UserAccountId { get; private set; }

    public UserRole Role { get; private set; }
}
