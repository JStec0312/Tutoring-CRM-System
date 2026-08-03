using Tutoring.Domain.Billing;

namespace Tutoring.Domain.TutoringAgreements;

public sealed record HourlyRate
{
    private HourlyRate()
    {
    }

    public HourlyRate(Money pricePerHour)
    {
        PricePerHour = pricePerHour ?? throw new ArgumentNullException(nameof(pricePerHour));
    }

    public Money PricePerHour { get; private set; } = null!;
}
