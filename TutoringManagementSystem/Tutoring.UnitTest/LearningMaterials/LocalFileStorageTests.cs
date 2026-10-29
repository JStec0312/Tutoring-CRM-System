using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tutoring.Infrastructure.FileStorage;

namespace Tutoring.UnitTest.LearningMaterials;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "tutoring-storage-tests", Guid.NewGuid().ToString("N"));
    private LocalFileStorage Storage() => new(Options.Create(new FileStorageOptions { RootPath = _root }), new TestEnvironment());

    [Fact]
    public async Task Save_ShouldRoundTripAndDelete()
    {
        var storage = Storage();
        var data = "%PDF-1.7"u8.ToArray();
        var key = await storage.SaveAsync(new MemoryStream(data), ".pdf", default);
        Assert.EndsWith(".pdf", key);
        await using (var stream = await storage.OpenReadAsync(key, default))
        {
            using var result = new MemoryStream();
            await stream.CopyToAsync(result);
            Assert.Equal(data, result.ToArray());
        }
        await storage.DeleteAsync(key, default);
        Assert.Empty(Directory.GetFiles(_root));
    }

    [Theory]
    [InlineData("../outside.pdf")]
    [InlineData("..\\outside.pdf")]
    [InlineData("/outside.pdf")]
    public async Task Open_ShouldRejectTraversal(string key) =>
        await Assert.ThrowsAsync<ArgumentException>(() => Storage().OpenReadAsync(key, default));

    [Fact]
    public async Task Save_WhenCopyFails_ShouldRemovePartialFile()
    {
        var storage = Storage();
        await Assert.ThrowsAsync<IOException>(() => storage.SaveAsync(new FailingStream(), ".pdf", default));
        Assert.Empty(Directory.GetFiles(_root));
    }

    [Fact]
    public async Task Save_WhenCancelled_ShouldRemovePartialFile()
    {
        var storage = Storage();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            storage.SaveAsync(new MemoryStream(new byte[32]), ".pdf", cancellation.Token));
        Assert.Empty(Directory.GetFiles(_root));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private sealed class FailingStream : MemoryStream
    {
        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            await destination.WriteAsync(new byte[8], cancellationToken);
            throw new IOException("Simulated copy failure.");
        }
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Tests";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
