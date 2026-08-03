using Tutoring.Domain.Common;

namespace Tutoring.Domain.Lessons;

public sealed record CancellationReason
{
    private CancellationReason()
    {
    }

    public CancellationReason(string text)
    {
        Text = Guard.NotBlank(text, nameof(text));
    }

    public string Text { get; private set; } = null!;
}
