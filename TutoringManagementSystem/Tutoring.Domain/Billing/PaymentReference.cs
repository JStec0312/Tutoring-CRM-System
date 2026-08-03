using Tutoring.Domain.Common;

namespace Tutoring.Domain.Billing;

public sealed record PaymentReference
{
    private PaymentReference()
    {
    }

    public PaymentReference(string value)
    {
        Value = Guard.NotBlank(value, nameof(value));
    }

    public string Value { get; private set; } = null!;
}
