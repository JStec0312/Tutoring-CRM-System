using Tutoring.Domain.Common;
using Tutoring.Domain.Identity;

namespace Tutoring.Domain.Students;

public sealed class Student
    : Entity<StudentId>
{
    private Student()
    {
    }

    public UserAccountId UserAccountId { get; private set; }
    public UserAccount Account { get; private set; } = null!;
    public StudentStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Student(UserAccountId userAccountId, DateTimeOffset createdAtUtc)
    {
        UserAccountId = userAccountId;
        Status = StudentStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }
}
