using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.LearningMaterials.Exceptions;

public sealed class MaterialContextNotFoundException(string detail)
    : NotFoundException("LearningMaterials.ContextNotFound", detail);
