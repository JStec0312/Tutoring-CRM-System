using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.LearningMaterials.DeleteLearningMaterial;

public sealed record DeleteLearningMaterialCommand(
    UserAccountId UserAccountId, Guid MaterialId, bool IsTutor, RequestMetadata RequestMetadata) : IRequest;
