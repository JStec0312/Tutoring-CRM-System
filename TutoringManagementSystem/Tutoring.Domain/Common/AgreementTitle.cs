public sealed record AgreementTitle
{
    private AgreementTitle()
    {
    }

    public AgreementTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "Agreement title cannot be empty.",
                nameof(value));

        var trimmed = value.Trim();

        if (trimmed.Length > 100)
            throw new ArgumentException(
                "Agreement title cannot exceed 100 characters.",
                nameof(value));

        Value = trimmed;
    }

    public string Value { get; private set; } = null!;
}