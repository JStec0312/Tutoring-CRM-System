using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Tutoring.Infrastructure.FileStorage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(
        IOptions<FileStorageOptions> options,
        IHostEnvironment environment)
    {
        var configuredPath = options.Value.RootPath;
        // Determining if path is rooted or relative
        // If the configured path is not rooted, combine it with the content root path of the application.
        // This ensures that the root path is always an absolute path.
        _rootPath = Path.IsPathRooted(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.GetFullPath(
                Path.Combine(environment.ContentRootPath, configuredPath));

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var extension = NormalizeExtension(fileExtension);
        var storageKey = $"{Guid.NewGuid()}{extension}"; 
        var filePath = GetFullPath(storageKey);
        var created = false;
        try
        {
            await using var fileStream = new FileStream(
                filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 81920, useAsync: true);
            created = true;
            await content.CopyToAsync(fileStream, cancellationToken);
            return storageKey;
        }
        catch
        {
            // The destination stream is disposed before cleanup, including on cancellation.
            if (created) File.Delete(filePath);
            throw;
        }
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var filePath = GetFullPath(storageKey);
        var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);
        return Task.FromResult<Stream>(fileStream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var filePath = GetFullPath(storageKey);
        File.Delete(filePath);
        return Task.CompletedTask;
    }


    private static string NormalizeExtension(string fileExtension)
    {
        // 1. Is the file extension null or whitespace? If so, return an empty string.
        // 2. Trim the file extension and ensure it starts with a dot.
        // 3. Ensure the file extension does not contain any directory separator characters.
        // 4. Convert the file extension to lowercase for consistency.
        
        if (string.IsNullOrWhiteSpace(fileExtension))
        {
            return string.Empty;
        }

        var extension = fileExtension.Trim();

        if (!extension.StartsWith('.'))
        {
            extension = $".{extension}";
        }

        if (extension.Skip(1).Any(character => !char.IsAsciiLetterOrDigit(character)))
        {
            throw new ArgumentException(
                "Invalid file extension.",
                nameof(fileExtension));
        }

        return extension.ToLowerInvariant();
    }

    private string GetFullPath(string storageKey)
    {
        // Path traversal protection: ensure the resulting full path is within the root path.
        // 1. Ensure the storage key is not null or whitespace.
        // 2. Combine the root path with the storage key to get the full path.
        // 3. Ensure the full path starts with the root path to prevent path traversal attacks.
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        if (storageKey.Contains('/') || storageKey.Contains('\\') || Path.IsPathRooted(storageKey))
            throw new ArgumentException("Invalid storage key.", nameof(storageKey));
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, storageKey));
        var rootPathWithSeparator = _rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
         if (!fullPath.StartsWith(
                rootPathWithSeparator,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Storage key resolves outside the storage root.",
                nameof(storageKey));
        }

        return fullPath;
    }
}
