using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Common.Authorization;
using Tutoring.Api.Features.LearningMaterials.Exceptions;
using Tutoring.Domain.LearningMaterials;
using Tutoring.Infrastructure.FileStorage;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.LearningMaterials.DownloadLearningMaterial;

public sealed class DownloadLearningMaterialHandler(
    TutoringDbContext dbContext,
    IFileStorage fileStorage,
    ILogger<DownloadLearningMaterialHandler> logger)
    : IRequestHandler<DownloadLearningMaterialQuery, DownloadLearningMaterialResponse>
{
    public async Task<DownloadLearningMaterialResponse> Handle(
        DownloadLearningMaterialQuery request, CancellationToken cancellationToken)
    {
        var materialId = new LearningMaterialId(request.MaterialId);
        var accessibleAgreementIds = dbContext.TutoringAgreements
            .ForParticipant(request.UserAccountId)
            .Select(agreement => agreement.Id);

        var material = await dbContext.LearningMaterials.AsNoTracking().SingleOrDefaultAsync(
            material => material.Id == materialId &&
                (material.Status == MaterialStatus.Draft || material.Status == MaterialStatus.Published) &&
                accessibleAgreementIds.Contains(material.TutoringAgreementId),
            cancellationToken);

        if (material is null)
            throw new MaterialContextNotFoundException("The learning material was not found or is not accessible to you.");

        Stream content;
        try
        {
            content = await fileStorage.OpenReadAsync(material.File.StorageLocation.Value, cancellationToken);
        }
        catch (IOException exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new MaterialFileNotFoundException();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            throw new MaterialDownloadFailedException(exception);
        }

        logger.LogInformation(
            "Opened learning material {MaterialId} for download by {UserAccountId} in agreement {TutoringAgreementId}. RequestMetadata: {RequestMetadata}",
            materialId.Value, request.UserAccountId.Value, material.TutoringAgreementId.Value, request.RequestMetadata);

        return new DownloadLearningMaterialResponse(content, material.File.ContentType, material.File.FileName);
    }
}
