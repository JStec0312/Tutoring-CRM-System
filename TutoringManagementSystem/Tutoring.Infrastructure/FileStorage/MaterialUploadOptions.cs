namespace Tutoring.Infrastructure.FileStorage;

public sealed class MaterialUploadOptions
{
    public const string SectionName = "MaterialUpload";
    public string[] AllowedExtensions { get; init; } = [];
    public long MaxSizeBytes { get; init; } = 20_000_000;

    public static bool IsValid(MaterialUploadOptions options) =>
        options.MaxSizeBytes > 0 && options.MaxSizeBytes <= long.MaxValue - 65_536 &&
        options.AllowedExtensions.Length > 0 &&
        options.AllowedExtensions.All(extension =>
            extension is ".pdf" or ".jpg" or ".png");
}
