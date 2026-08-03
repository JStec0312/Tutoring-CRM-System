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

    public PersonalProfile Profile { get; private set; } = null!;

    public AccountStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<UserAccountRole> RoleAssignments => _roleAssignments.AsReadOnly();

    public IReadOnlyCollection<UserRole> Roles => _roleAssignments
        .Select(roleAssignment => roleAssignment.Role)
        .ToArray();
}
