using Tutoring.Domain.Billing;
using Tutoring.Domain.TutoringAgreements;

namespace Tutoring.UnitTest.TutoringAgreements;

public sealed class HourlyRateTest
{
    [Theory]
    [InlineData(100, 60, 100)]
    [InlineData(100, 30, 50)]
    [InlineData(100, 90, 150)]
    [InlineData(100, 45, 75)]
    [InlineData(99.99, 20, 33.33)]
    public void CalculateCost_ShouldCalculateProportionalAmount(
        decimal pricePerHour,
        int durationMinutes,
        decimal expectedAmount)
    {
        var rate = new HourlyRate(new Money(pricePerHour, new Currency("PLN")));

        var result = rate.CalculateCost(TimeSpan.FromMinutes(durationMinutes));

        Assert.Equal(expectedAmount, result.Amount);
    }

    [Fact]
    public void CalculateCost_ShouldRoundToTwoDecimalPlacesAndPreserveCurrency()
    {
        var currency = new Currency("EUR");
        var rate = new HourlyRate(new Money(100, currency));

        var result = rate.CalculateCost(TimeSpan.FromMinutes(1));

        Assert.Equal(1.67m, result.Amount);
        Assert.Equal(currency, result.Currency);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CalculateCost_WithNonPositiveDuration_ShouldThrow(int durationMinutes)
    {
        var rate = new HourlyRate(new Money(100, new Currency("PLN")));

        Assert.Throws<InvalidTimeToCalculateCostException>(
            () => rate.CalculateCost(TimeSpan.FromMinutes(durationMinutes)));
    }
}
