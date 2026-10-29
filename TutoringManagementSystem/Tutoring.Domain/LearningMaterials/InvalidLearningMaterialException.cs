using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Domain.LearningMaterials;

public sealed class InvalidLearningMaterialException(string message)
    : DomainException("LearningMaterials.InvalidMetadata", message);
