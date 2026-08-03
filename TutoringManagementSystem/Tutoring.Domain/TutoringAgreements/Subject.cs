using Tutoring.Domain.Common;

namespace Tutoring.Domain.TutoringAgreements;

public sealed record Subject
{
    private Subject()
    {
    }

    public Subject(string name)
    {
        Name = Guard.NotBlank(name, nameof(name));
    }

    public string Name { get; private set; } = null!;
}
