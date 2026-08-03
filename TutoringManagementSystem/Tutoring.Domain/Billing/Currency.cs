using Tutoring.Domain.Common;

namespace Tutoring.Domain.Billing;

public sealed record Currency
{
    private Currency()
    {
    }

    public Currency(string code)
    {
        string normalizedCode = Guard.NotBlank(code, nameof(code)).ToUpperInvariant();

        if (normalizedCode.Length != 3)
        {
            throw new ArgumentException("Currency code must have exactly 3 characters.", nameof(code));
        }

        Code = normalizedCode;
    }

    public string Code { get; private set; } = null!;
}
