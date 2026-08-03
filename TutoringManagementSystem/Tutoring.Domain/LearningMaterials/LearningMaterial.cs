using Tutoring.Domain.Common;
using Tutoring.Domain.Tutors;

namespace Tutoring.Domain.LearningMaterials;

public sealed class LearningMaterial
    : Entity<LearningMaterialId>
{
    private LearningMaterial()
    {
    }

    public TutorId TutorId { get; private set; }

    public Tutor Tutor { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public FileDescriptor File { get; private set; } = null!;

    public MaterialStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
