namespace Tutoring.Api.Features.LearningMaterials.GetLearningMaterials;

public sealed record LearningMaterialResponse(
    Guid MaterialId,
    Guid? LessonId,
    string Title,
    string FileName,
    string ContentType,
    long SizeBytes,
    bool UploadedByTutor);
