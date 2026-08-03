namespace Tutoring.Domain.LearningMaterials;

public sealed record FileSize
{
    private FileSize()
    {
    }

    public FileSize(long bytes)
    {
        if (bytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), "File size cannot be negative.");
        }

        Bytes = bytes;
    }

    public long Bytes { get; private set; }
}
