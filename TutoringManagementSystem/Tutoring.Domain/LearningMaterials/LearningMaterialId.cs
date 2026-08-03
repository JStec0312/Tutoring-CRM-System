using Tutoring.Domain.Common;

namespace Tutoring.Domain.LearningMaterials;

public readonly record struct LearningMaterialId(Guid Value)
    : IDomainId
{
    public static LearningMaterialId New() => new(Guid.NewGuid());
}
