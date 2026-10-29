using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.LearningMaterials.Exceptions;

public sealed class MaterialContextNotFoundException()
    : NotFoundException("LearningMaterials.ContextNotFound", "The material context was not found.");
