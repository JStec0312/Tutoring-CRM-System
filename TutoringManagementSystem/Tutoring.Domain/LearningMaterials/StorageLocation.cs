using Tutoring.Domain.Common;

namespace Tutoring.Domain.LearningMaterials;

public sealed record StorageLocation
{
    private StorageLocation()
    {
    }

    public StorageLocation(string value)
    {
        Value = Guard.NotBlank(value, nameof(value));
    }

    public string Value { get; private set; } = null!;
}
