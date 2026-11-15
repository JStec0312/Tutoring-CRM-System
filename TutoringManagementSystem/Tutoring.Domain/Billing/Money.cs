namespace Tutoring.Domain.Billing;

public sealed record Money
{
    private Money()
    {
    }

    public Money(decimal amount, Currency currency)
    {
        SetAmount(amount);
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }

    public decimal Amount { get; private set; }

    public Currency Currency { get; private set; } = null!;

    internal void SetAmount(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        }

        Amount = amount;
    }
}
