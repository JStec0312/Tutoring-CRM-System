using Tutoring.Domain.Common;

namespace Tutoring.Domain.LearningMaterials;

public readonly record struct MaterialAssignmentId(Guid Value)
    : DomainId<MaterialAssignmentId>
{
    public static MaterialAssignmentId New() => new(Guid.NewGuid());
}
