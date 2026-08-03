using Tutoring.Domain.Common;

namespace Tutoring.Domain.LearningMaterials;

public sealed record FileDescriptor
{
    private FileDescriptor()
    {
    }

    public FileDescriptor(
        string fileName,
        string contentType,
        FileSize size,
        StorageLocation storageLocation)
    {
        FileName = Guard.NotBlank(fileName, nameof(fileName));
        ContentType = Guard.NotBlank(contentType, nameof(contentType));
        Size = size ?? throw new ArgumentNullException(nameof(size));
        StorageLocation = storageLocation ?? throw new ArgumentNullException(nameof(storageLocation));
    }

    public string FileName { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    public FileSize Size { get; private set; } = null!;

    public StorageLocation StorageLocation { get; private set; } = null!;
}
