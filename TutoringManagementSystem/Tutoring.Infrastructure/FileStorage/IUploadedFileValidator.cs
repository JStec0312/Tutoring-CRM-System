namespace Tutoring.Infrastructure.FileStorage;

public sealed record ValidatedUpload(string FileName, string Extension, string ContentType, long SizeBytes);

public interface IUploadedFileValidator
{
    Task<ValidatedUpload> ValidateAsync(
        Stream content, string fileName, long declaredSize, CancellationToken cancellationToken);
}
