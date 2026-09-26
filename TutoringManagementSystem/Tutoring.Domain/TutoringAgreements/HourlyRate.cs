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
        var hours = (decimal)duration.TotalMinutes / 60;
        var amount = decimal.Round(PricePerHour.Amount * hours, 2, MidpointRounding.AwayFromZero); // 2.4 -> 2 2.5 -> 3 
        return new Money(amount, PricePerHour.Currency); 
    }
}
