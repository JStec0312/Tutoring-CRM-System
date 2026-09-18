using Tutoring.Domain.Common;
using Tutoring.Domain.TutoringAgreements;

namespace Tutoring.Domain.LessonSeries;

public sealed class LessonSeries : Entity<LessonSeriesId>
{
    private LessonSeries()
    {
    }

    public TutoringAgreementId TutoringAgreementId { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }

    public TimeOnly StartsAt { get; private set; }

    public TimeSpan Duration { get; private set; }

    public DateOnly StartsOn { get; private set; }

    public DateOnly? EndsOn { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public LessonSeries(
        TutoringAgreementId tutoringAgreementId,
        DayOfWeek dayOfWeek,
        TimeOnly startsAt,
        TimeSpan duration,
        DateOnly startsOn,
        DateOnly? endsOn,
        DateTimeOffset createdAtUtc)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        if (endsOn.HasValue && endsOn.Value < startsOn)
        {
            throw new ArgumentException(
                "Series end date cannot be before start date.",
                nameof(endsOn));
        }

        TutoringAgreementId = tutoringAgreementId;
        DayOfWeek = dayOfWeek;
        StartsAt = startsAt;
        Duration = duration;
        StartsOn = startsOn;
        EndsOn = endsOn;
        CreatedAtUtc = createdAtUtc;
    }
}