using Tutoring.Domain.Common;
using Tutoring.Domain.Students;
using Tutoring.Domain.Tutors;

namespace Tutoring.Domain.TutoringAgreements;

public sealed class TutoringAgreement
    : Entity<TutoringAgreementId>
{
    private TutoringAgreement()
    {
    }

    public TutorId TutorId { get; private set; }
    public Tutor Tutor { get; private set; } = null!;

    public StudentId StudentId { get; private set; }
    public Student Student { get; private set; } = null!;

    public Subject Subject { get; private set; } = null!;

    public HourlyRate HourlyRate { get; private set; } = null!;

    public AgreementStatus Status { get; private set; }

    public string? PrivateNotes { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
