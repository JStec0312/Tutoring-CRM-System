using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.LearningMaterials.AttachLearningMaterialToLesson;

public sealed record AttachLearningMaterialToLessonCommand(
    UserAccountId UserAccountId, Guid MaterialId, Guid LessonId, RequestMetadata RequestMetadata) : IRequest;
