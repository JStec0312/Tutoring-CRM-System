using Tutoring.Domain.Common;
using Tutoring.Domain.Identity;

namespace Tutoring.Domain.Students;

public sealed class Student
    : Entity<StudentId>
{
    private Student()
    {
    }

    public UserAccountId? UserAccountId { get; private set; }

    public UserAccount? Account { get; private set; }

    public StudentDisplayName DisplayName { get; private set; } = null!;

    public StudentStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Student(
        UserAccountId userAccountId,
        StudentDisplayName displayName,
        DateTimeOffset createdAtUtc)
    {
        UserAccountId = userAccountId;
        DisplayName = displayName;
        Status = StudentStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    public Student(
        StudentDisplayName displayName,
        DateTimeOffset createdAtUtc)
    {
        UserAccountId = null;
        DisplayName = displayName;
        Status = StudentStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }
}