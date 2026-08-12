namespace Tutoring.Domain.Common;

public sealed record PhoneNumber
{
    private PhoneNumber()
    {
    }

    public PhoneNumber(string value)
    {
        Value = Guard.NotBlank(value, nameof(value));
    }

    public string Value { get; private set; } = null!;
}
