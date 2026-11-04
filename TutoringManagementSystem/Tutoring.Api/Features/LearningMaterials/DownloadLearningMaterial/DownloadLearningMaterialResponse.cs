namespace Tutoring.Api.Features.LearningMaterials.DownloadLearningMaterial;

public sealed record DownloadLearningMaterialResponse(Stream Content, string ContentType, string FileName);
