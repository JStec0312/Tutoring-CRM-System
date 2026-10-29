namespace Tutoring.Api.Features.LearningMaterials.UploadLearningMaterial;

public sealed record UploadLearningMaterialResponse(
    Guid MaterialId, Guid TutoringAgreementId, Guid? LessonId, Guid UploadedByUserAccountId,
    string Title, string FileName, string ContentType, long SizeBytes, string Status, DateTimeOffset CreatedAtUtc);
