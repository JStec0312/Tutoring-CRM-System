using Tutoring.Domain.Common;
using Tutoring.Domain.Tutors;

namespace Tutoring.Domain.StudentInvitations;

public sealed class StudentInvitation
    : Entity<StudentInvitationId>
{
    private StudentInvitation()
    {
    }

    public TutorId TutorId { get; private set; }

    public Tutor Tutor { get; private set; } = null!;

    public EmailAddress Recipient { get; private set; } = null!;

    public InvitationStatus Status { get; private set; }

    public DateTimeOffset ValidUntilUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
