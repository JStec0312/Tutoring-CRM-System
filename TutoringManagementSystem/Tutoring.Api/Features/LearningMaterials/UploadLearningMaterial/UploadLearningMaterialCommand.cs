using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.LearningMaterials.UploadLearningMaterial;

public sealed record UploadLearningMaterialCommand(
    UserAccountId UserAccountId, Guid TutoringAgreementId, Guid? LessonId,
    string Title, Stream Content, string FileName, long SizeBytes, RequestMetadata RequestMetadata)
    : IRequest<UploadLearningMaterialResponse>;
