using Tutoring.Domain.Common;
using Tutoring.Domain.TutoringAgreements;

namespace Tutoring.Domain.Lessons;

public sealed class Lesson
    : Entity<LessonId>
{
    private Lesson()
    {
    }

    public TutoringAgreementId TutoringAgreementId { get; private set; }

    public TutoringAgreement Agreement { get; private set; } = null!;

    public TimeSlot TimeSlot { get; private set; } = null!;

    public LessonStatus Status { get; private set; }

    public CancellationReason? CancellationReason { get; private set; }

    public LessonNote? Note { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
