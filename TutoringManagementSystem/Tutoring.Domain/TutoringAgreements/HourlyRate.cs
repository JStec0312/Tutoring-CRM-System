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

    public Money CalculateCost(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new InvalidTimeToCalculateCostException();
        }
        var hours = (decimal)duration.TotalMinutes / 60m;
        var amount = decimal.Round(PricePerHour.Amount * hours, 2, MidpointRounding.AwayFromZero);
        return new Money(amount, PricePerHour.Currency); 
    }
}
