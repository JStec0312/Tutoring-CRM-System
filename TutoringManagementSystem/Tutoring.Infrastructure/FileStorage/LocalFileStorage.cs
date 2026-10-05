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
        await using var fileStream = new FileStream(
            filePath, // Full path to the file to be created
            FileMode.CreateNew, // Create a new file. If the file already exists, an IOException will be thrown.
            FileAccess.Write, // Allow writing to the file
            FileShare.None, // Lock the file for exclusive access while writing
            bufferSize: 81920, // Default buffer size for asynchronous operations
            useAsync: true); // Enable asynchronous I/O operations
        await content.CopyToAsync(fileStream, cancellationToken); // Copy the content to the file asynchronously 
        return storageKey; 
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
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

        if (extension.Contains(Path.DirectorySeparatorChar) ||
            extension.Contains(Path.AltDirectorySeparatorChar))
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
        ArgumentNullException.ThrowIfNullOrWhiteSpace(storageKey);
        var fullPath = Path.Combine(_rootPath, storageKey);
        var rootPathWithSeparator = _rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
         if (!fullPath.StartsWith(
                rootPathWithSeparator,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Storage key resolves outside the storage root.",
                nameof(storageKey));
        }

        return fullPath;
    }
}