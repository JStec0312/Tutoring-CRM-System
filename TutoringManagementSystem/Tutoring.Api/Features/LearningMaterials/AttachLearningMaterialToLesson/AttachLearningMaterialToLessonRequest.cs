using System.ComponentModel.DataAnnotations;

namespace Tutoring.Api.Features.LearningMaterials.AttachLearningMaterialToLesson;

public sealed class AttachLearningMaterialToLessonRequest
{
    [Required]
    public Guid? LessonId { get; init; }
}
