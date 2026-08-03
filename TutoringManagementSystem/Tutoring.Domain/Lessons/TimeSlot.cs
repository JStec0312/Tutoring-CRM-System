namespace Tutoring.Domain.Lessons;

public sealed record TimeSlot
{
    private TimeSlot()
    {
    }

    public TimeSlot(
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc)
    {
        if (endsAtUtc <= startsAtUtc)
        {
            throw new ArgumentException("End time must be later than start time.", nameof(endsAtUtc));
        }

        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
    }

    public DateTimeOffset StartsAtUtc { get; private set; }

    public DateTimeOffset EndsAtUtc { get; private set; }
}
