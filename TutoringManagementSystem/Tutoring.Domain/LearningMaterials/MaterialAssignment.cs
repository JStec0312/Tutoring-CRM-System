using Tutoring.Domain.Common;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.TutoringAgreements;

namespace Tutoring.Domain.LearningMaterials;

public sealed class MaterialAssignment
    : Entity<MaterialAssignmentId>
{
    private MaterialAssignment()
    {
    }

    public LearningMaterialId LearningMaterialId { get; private set; }
    public LearningMaterial LearningMaterial { get; private set; } = null!;

    public TutoringAgreementId TutoringAgreementId { get; private set; }
    public TutoringAgreement Agreement { get; private set; } = null!;

    public LessonId? LessonId { get; private set; }
    public Lesson? Lesson { get; private set; }
    public AssignmentStatus Status { get; private set; }

    public DateTimeOffset AssignedAtUtc { get; private set; }
}
