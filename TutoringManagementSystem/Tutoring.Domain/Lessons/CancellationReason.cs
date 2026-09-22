using Tutoring.Domain.Common;

namespace Tutoring.Domain.Lessons;

public sealed record CancellationReason
{
    private CancellationReason()
    {
    }
    private const int MaxLength = 500;

    public CancellationReason(string text)
    {
        Text = Guard.NotBlank(text, nameof(text));
        if(Text.Length > MaxLength)
        {
            throw new ArgumentException($"The length of {nameof(text)} cannot exceed {MaxLength} characters.", nameof(text));
        }
    }

    public string Text { get; private set; } = null!;
}
