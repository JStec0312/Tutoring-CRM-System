using Tutoring.Domain.Common;
using Tutoring.Domain.Identity;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.TutoringAgreements;

namespace Tutoring.Domain.LearningMaterials;

public sealed class LearningMaterial
    : Entity<LearningMaterialId>
{
    private LearningMaterial()
    {
    }

    public TutoringAgreementId TutoringAgreementId { get; private set; }
    public LessonId? LessonId { get; private set; }
    public UserAccountId UploadedByUserAccountId { get; private set; }
    public string Title { get; private set; } = null!;

    public FileDescriptor File { get; private set; } = null!;

    public MaterialStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public LearningMaterial(
        TutoringAgreementId tutoringAgreementId, LessonId? lessonId,
        UserAccountId uploadedByUserAccountId, string title, FileDescriptor file,
        DateTimeOffset createdAtUtc)
    {
        ValidateTitle(title);
        if (tutoringAgreementId.Value == Guid.Empty || uploadedByUserAccountId.Value == Guid.Empty ||
            lessonId is { Value: var value } && value == Guid.Empty)
            throw new InvalidLearningMaterialException("Material context and uploader IDs must not be empty.");
        ArgumentNullException.ThrowIfNull(file);
        if (file.Size.Bytes <= 0 || file.FileName.Length > 260 || file.ContentType.Length > 255 ||
            file.StorageLocation.Value.Length > 1024)
            throw new InvalidLearningMaterialException("File metadata is invalid.");

        TutoringAgreementId = tutoringAgreementId;
        LessonId = lessonId;
        UploadedByUserAccountId = uploadedByUserAccountId;
        Title = title.Trim();
        File = file;
        Status = MaterialStatus.Draft;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
    }

    public static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200)
            throw new InvalidLearningMaterialException("The title must contain between 1 and 200 characters.");
    }
}
