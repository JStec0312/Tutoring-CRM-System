namespace Tutoring.Api.Features.LearningMaterials.Exceptions;

public sealed class MaterialDownloadFailedException(Exception innerException)
    : Exception("The learning material file could not be opened.", innerException)
{
    public const string Code = "LearningMaterials.DownloadFailed";
}
