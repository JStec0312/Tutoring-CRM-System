using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Tutoring.Infrastructure.FileStorage;

namespace Tutoring.UnitTest.LearningMaterials;

public sealed class UploadedFileValidatorTests
{
    [Fact]
    public void Configuration_ShouldReplaceAllowedFormatsWithoutImplicitDefaults()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MaterialUpload:AllowedExtensions:0"] = ".png",
            ["MaterialUpload:MaxSizeBytes"] = "8"
        }).Build();
        var options = config.GetSection(MaterialUploadOptions.SectionName).Get<MaterialUploadOptions>()!;
        Assert.Equal(new[] { ".png" }, options.AllowedExtensions);
        Assert.Equal(8, options.MaxSizeBytes);
        Assert.True(MaterialUploadOptions.IsValid(options));
    }

    private static UploadedFileValidator Validator(long max = 20_000_000, string[]? allowed = null) =>
        new(Options.Create(new MaterialUploadOptions { MaxSizeBytes = max, AllowedExtensions = allowed ?? [".pdf", ".jpg", ".png"] }));

    [Theory]
    [InlineData(".pdf", "application/pdf")]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".png", "image/png")]
    public async Task Validate_ShouldRecognizeConfiguredFormatAndRewind(string extension, string mime)
    {
        var bytes = extension switch
        {
            ".pdf" => "%PDF-1.7"u8.ToArray(),
            ".jpg" => new byte[] { 255, 216, 255, 224 },
            _ => new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }
        };
        using var stream = new MemoryStream(bytes);
        var result = await Validator().ValidateAsync(stream, "C:\\fakepath\\NOTES" + extension.ToUpperInvariant(), bytes.Length, default);
        Assert.Equal(mime, result.ContentType);
        Assert.Equal("NOTES" + extension.ToUpperInvariant(), result.FileName);
        Assert.Equal(0, stream.Position);
        Assert.Equal(bytes, stream.ToArray());
    }

    [Theory]
    [InlineData(0, "LearningMaterials.EmptyFile")]
    [InlineData(9, "LearningMaterials.FileTooLarge")]
    public async Task Validate_ShouldRejectInvalidSize(long declared, string code)
    {
        using var stream = new MemoryStream("%PDF-1.7"u8.ToArray());
        var error = await Assert.ThrowsAnyAsync<UploadValidationException>(() =>
            Validator(max: 8).ValidateAsync(stream, "notes.pdf", declared, default));
        Assert.Equal(code, error.Code);
    }

    [Fact]
    public async Task Validate_ShouldAcceptExactSizeLimit()
    {
        using var stream = new MemoryStream("%PDF-1.7"u8.ToArray());
        Assert.Equal(8, (await Validator(max: 8).ValidateAsync(stream, "notes.pdf", 8, default)).SizeBytes);
    }

    [Fact]
    public async Task Validate_ShouldRespectDisabledFormat()
    {
        using var stream = new MemoryStream("%PDF-1.7"u8.ToArray());
        var error = await Assert.ThrowsAsync<UploadValidationException>(() =>
            Validator(allowed: [".png"]).ValidateAsync(stream, "notes.pdf", 8, default));
        Assert.Equal("LearningMaterials.UnsupportedFormat", error.Code);
    }

    [Theory]
    [InlineData("fake.pdf", "LearningMaterials.InvalidFileContent")]
    [InlineData("file.exe", "LearningMaterials.UnsupportedFormat")]
    [InlineData("   ", "LearningMaterials.InvalidFileName")]
    public async Task Validate_ShouldRejectInvalidFile(string name, string code)
    {
        using var stream = new MemoryStream(new byte[8]);
        var error = await Assert.ThrowsAsync<UploadValidationException>(() =>
            Validator().ValidateAsync(stream, name, 8, default));
        Assert.Equal(code, error.Code);
    }

    [Fact]
    public async Task Validate_ShouldRejectLengthMismatch()
    {
        using var stream = new MemoryStream("%PDF-1.7"u8.ToArray());
        var error = await Assert.ThrowsAsync<UploadValidationException>(() =>
            Validator().ValidateAsync(stream, "notes.pdf", 7, default));
        Assert.Equal("LearningMaterials.InvalidFile", error.Code);
    }
}
