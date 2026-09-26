using Tutoring.Domain.Common;
using Tutoring.Domain.Identity;
using Tutoring.Domain.LessonSeries;
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
    public LessonSeriesId? LessonSeriesId { get; private set; }
    public TimeSlot TimeSlot { get; private set; } = null!;
    public LessonCancellationParty? LessonCancellationParty { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public UserAccountId? CancelledByUserAccountId { get; private set; }
    public LessonStatus Status { get; private set; }

    public CancellationReason? CancellationReason { get; private set; }

    public LessonNote? Note { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Lesson(
        TutoringAgreementId tutoringAgreementId,
        TimeSlot timeSlot,
        DateTimeOffset createdAtUtc,
        LessonSeriesId? lessonSeriesId = null)
    {
        TutoringAgreementId = tutoringAgreementId;
        TimeSlot = timeSlot ?? throw new ArgumentNullException(nameof(timeSlot));

        LessonSeriesId = lessonSeriesId;

        Status = LessonStatus.Scheduled;
        CreatedAtUtc = createdAtUtc;
    }

    public void Reschedule(TimeSlot newTimeSlot)
    {
        if(Status != LessonStatus.Scheduled)
        {
            throw new CanNotRescheduleInactiveLessonException();
        }

        TimeSlot = newTimeSlot;
    }

    public void Cancel(
        LessonCancellationParty lessonCancellationParty,
        CancellationReason? cancellationReason,
        UserAccountId cancelledByUserAccountId,
        DateTimeOffset cancelledAtUtc)
    {
        if(Status != LessonStatus.Scheduled)
        {
            throw new CanNotCancelActiveLessonException();
        }

        Status = LessonStatus.Cancelled;
        LessonCancellationParty = lessonCancellationParty;
        CancellationReason = cancellationReason;
        CancelledAtUtc = cancelledAtUtc;
        CancelledByUserAccountId = cancelledByUserAccountId;
    }

    public void Complete(DateTimeOffset nowUtc)
    {
        EnsureCanBeFinalized(nowUtc);

        Status = LessonStatus.Completed;
    }

    public void MarkAsMissed(DateTimeOffset nowUtc)
    {
        EnsureCanBeFinalized(nowUtc);

        Status = LessonStatus.Missed;
    }

    private void EnsureCanBeFinalized(DateTimeOffset nowUtc)
    {
        if (Status != LessonStatus.Scheduled)
        {
            throw new CanNotFinalizeLessonException(Status);
        }

        if (nowUtc < TimeSlot.EndsAtUtc)
        {
            throw new CanNotFinalizeLessonBeforeEndException();
        }
    }

    public bool IsCompleted => Status == LessonStatus.Completed;

}
