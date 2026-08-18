using Tutoring.Domain.Common;
using Tutoring.Domain.Identity;

namespace Tutoring.Domain.Tutors;

public sealed class Tutor
    : Entity<TutorId>
{
    private Tutor()
    {
    }

    public UserAccountId UserAccountId { get; private set; }
    public UserAccount Account { get; private set; } = null!;
    public TutorStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Tutor(UserAccountId userAccountId)
    {
        UserAccountId = userAccountId;
        Status = TutorStatus.Active;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }
}
