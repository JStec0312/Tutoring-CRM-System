using Tutoring.Domain.Identity;
using Tutoring.Domain.LearningMaterials;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.TutoringAgreements;

namespace Tutoring.UnitTest.LearningMaterials;

public sealed class LearningMaterialTests
{
    private static FileDescriptor File(long bytes = 10, string name = "notes.pdf") =>
        new(name, "application/pdf", new FileSize(bytes), new StorageLocation("key.pdf"));

    [Fact]
    public void Create_ShouldPreserveContextAndUploaderAndStartAsDraft()
    {
        var ta = TutoringAgreementId.New();
        var lesson = LessonId.New();
        var uploader = UserAccountId.New();
        var now = new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.FromHours(2));
        var material = new LearningMaterial(ta, lesson, uploader, " Notes ", File(), now);
        Assert.Equal(ta, material.TutoringAgreementId);
        Assert.Equal(lesson, material.LessonId);
        Assert.Equal(uploader, material.UploadedByUserAccountId);
        Assert.Equal("Notes", material.Title);
        Assert.Equal(MaterialStatus.Draft, material.Status);
        Assert.Equal(TimeSpan.Zero, material.CreatedAtUtc.Offset);
        Assert.Equal(now, material.CreatedAtUtc);
        Assert.NotEqual(Guid.Empty, material.Id.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyTitle_ShouldReject(string? title) =>
        Assert.Throws<InvalidLearningMaterialException>(() =>
            new LearningMaterial(TutoringAgreementId.New(), null, UserAccountId.New(), title!, File(), DateTimeOffset.UtcNow));

    [Fact]
    public void Create_WithTooLongTitle_ShouldReject() =>
        Assert.Throws<InvalidLearningMaterialException>(() =>
            new LearningMaterial(TutoringAgreementId.New(), null, UserAccountId.New(), new string('a', 201), File(), DateTimeOffset.UtcNow));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Create_WithEmptyContextId_ShouldReject(int empty)
    {
        Assert.Throws<InvalidLearningMaterialException>(() => new LearningMaterial(
            empty == 0 ? default : TutoringAgreementId.New(),
            empty == 1 ? new LessonId(Guid.Empty) : null,
            empty == 2 ? default : UserAccountId.New(), "Notes", File(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_WithEmptyFile_ShouldReject() =>
        Assert.Throws<InvalidLearningMaterialException>(() =>
            new LearningMaterial(TutoringAgreementId.New(), null, UserAccountId.New(), "Notes", File(0), DateTimeOffset.UtcNow));

    [Fact]
    public void Create_WithTooLongFileName_ShouldReject() =>
        Assert.Throws<InvalidLearningMaterialException>(() =>
            new LearningMaterial(TutoringAgreementId.New(), null, UserAccountId.New(), "Notes", File(name: new string('a', 261)), DateTimeOffset.UtcNow));

    [Fact]
    public void Create_WithNullFile_ShouldReject() =>
        Assert.Throws<ArgumentNullException>(() =>
            new LearningMaterial(TutoringAgreementId.New(), null, UserAccountId.New(), "Notes", null!, DateTimeOffset.UtcNow));
}
