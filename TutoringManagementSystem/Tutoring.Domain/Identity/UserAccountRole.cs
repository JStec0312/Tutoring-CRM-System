namespace Tutoring.Domain.Identity;

public sealed class UserAccountRole
{
    private UserAccountRole()
    {
    }

    public UserAccountId UserAccountId { get; private set; }

    public UserRole Role { get; private set; }
}
