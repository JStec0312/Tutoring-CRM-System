using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.LearningMaterials.DownloadLearningMaterial;

public sealed record DownloadLearningMaterialQuery(
    UserAccountId UserAccountId, Guid MaterialId, RequestMetadata RequestMetadata)
    : IRequest<DownloadLearningMaterialResponse>;
