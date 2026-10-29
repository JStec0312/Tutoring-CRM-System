using Microsoft.Extensions.Options;

namespace Tutoring.Infrastructure.FileStorage;

public sealed class UploadedFileValidator(IOptions<MaterialUploadOptions> options) : IUploadedFileValidator
{
    public async Task<ValidatedUpload> ValidateAsync(
        Stream content, string fileName, long declaredSize, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();
        var policy = options.Value;
        if (declaredSize > policy.MaxSizeBytes)
            throw new UploadTooLargeException();
        if (declaredSize <= 0)
            throw new UploadValidationException("LearningMaterials.EmptyFile", "The file must not be empty.");
        if (!content.CanSeek || content.Position != 0 || content.Length != declaredSize)
            throw new UploadValidationException("LearningMaterials.InvalidFile", "The buffered file length is invalid.");

        // Browser file names may contain Windows separators even when hosted on Linux.
        var name = Path.GetFileName(fileName.Replace('\\', '/')).Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 260 || name.Any(char.IsControl))
            throw new UploadValidationException("LearningMaterials.InvalidFileName", "The file name is invalid.");

        var extension = Path.GetExtension(name).ToLowerInvariant();
        if (!policy.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            throw new UploadValidationException("LearningMaterials.UnsupportedFormat", "The file format is not allowed.");

        var header = new byte[8];
        var read = await content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        content.Position = 0;
        var valid = extension switch
        {
            ".pdf" => read >= 5 && header.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            ".jpg" => read >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff,
            ".png" => read >= 8 && header.AsSpan().SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            _ => false
        };
        if (!valid)
            throw new UploadValidationException("LearningMaterials.InvalidFileContent", "The file signature does not match its format.");

        var contentType = extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" => "image/jpeg",
            _ => "image/png"
        };
        return new ValidatedUpload(name, extension, contentType, declaredSize);
    }
}
