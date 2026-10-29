namespace Tutoring.Api.Features.LearningMaterials.Exceptions;

public sealed class MaterialUploadFailedException(Exception innerException)
    : Exception("The learning material could not be saved.", innerException)
{
    public const string Code = "LearningMaterials.UploadFailed";
}
