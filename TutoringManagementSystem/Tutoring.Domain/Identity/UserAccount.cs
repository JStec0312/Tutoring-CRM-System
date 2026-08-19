using Tutoring.Domain.Common;

namespace Tutoring.Domain.Identity;

public sealed class UserAccount
    : Entity<UserAccountId>
{
    private readonly List<UserAccountRole> _roleAssignments = new();

    private UserAccount()
    {
    }

    public EmailAddress Email { get; private set; } = null!;

    public PasswordHash PasswordHash { get; private set; } = null!;

    public PersonalProfile Profile { get; private set; } = null!;

    public AccountStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public bool IsActive => Status == AccountStatus.Active;

    public IReadOnlyCollection<UserAccountRole> RoleAssignments => _roleAssignments.AsReadOnly();

    public IReadOnlyCollection<UserRole> Roles => _roleAssignments
        .Select(roleAssignment => roleAssignment.Role)
        .ToArray();
    
    public UserAccount(
        EmailAddress email,
        PasswordHash passwordHash,
        PersonalProfile profile        
        )
    {
        Email = email;
        PasswordHash = passwordHash;
        Status = AccountStatus.Active;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        Profile = profile;
    }

    public void AssignRole(UserRole role)
    {
        if (_roleAssignments.Any(roleAssignment => roleAssignment.Role == role))
        {
            return;
        }

        var roleAssignment = new UserAccountRole(this.Id, role);
        _roleAssignments.Add(roleAssignment);
    }
}
