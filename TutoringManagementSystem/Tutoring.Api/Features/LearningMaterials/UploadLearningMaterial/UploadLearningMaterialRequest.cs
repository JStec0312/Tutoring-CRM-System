using System.ComponentModel.DataAnnotations;

namespace Tutoring.Api.Features.LearningMaterials.UploadLearningMaterial;

public sealed class UploadLearningMaterialRequest
{
    [Required]
    public string Title { get; init; } = null!;
    [Required]
    public IFormFile File { get; init; } = null!;
    [Required]
    public Guid? TutoringAgreementId { get; init; }
    public Guid? LessonId { get; init; }
}
