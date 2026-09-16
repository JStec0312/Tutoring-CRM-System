namespace Tutoring.Domain.Students;

public sealed record StudentDisplayName
{
    private StudentDisplayName()
    {
    }

    public StudentDisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Student display name cannot be empty.",
                nameof(value));
        }

        var trimmed = value.Trim();

        if (trimmed.Length > 100)
        {
            throw new ArgumentException(
                "Student display name cannot exceed 100 characters.",
                nameof(value));
        }

        Value = trimmed;
    }

    public string Value { get; private set; } = null!;
}