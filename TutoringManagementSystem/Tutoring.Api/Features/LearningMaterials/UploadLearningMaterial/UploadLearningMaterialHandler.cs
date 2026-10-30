using Tutoring.Api.Features.Common.Authorization;
using MediatR;
using Tutoring.Api.Features.LearningMaterials.Exceptions;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.LearningMaterials;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.FileStorage;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.LearningMaterials.UploadLearningMaterial;

public sealed class UploadLearningMaterialHandler(
    TutoringDbContext dbContext, IUploadedFileValidator validator, IFileStorage storage,
    TimeProvider timeProvider, ILogger<UploadLearningMaterialHandler> logger)
    : IRequestHandler<UploadLearningMaterialCommand, UploadLearningMaterialResponse>
{
    public async Task<UploadLearningMaterialResponse> Handle(
        UploadLearningMaterialCommand request, CancellationToken cancellationToken)
    {
        var agreementId = new TutoringAgreementId(request.TutoringAgreementId);
        var allowed = await dbContext.TutoringAgreements.AsNoTracking()
            .ForParticipant(request.UserAccountId)
            .AnyAsync(agreement => agreement.Id == agreementId, cancellationToken);
        if (!allowed)
            throw new MaterialContextNotFoundException("The tutoring agreement was not found or is not accessible to you.");

        LessonId? lessonId = request.LessonId is { } id ? new LessonId(id) : null;
        if (lessonId is not null && !await dbContext.Lessons.AsNoTracking().AnyAsync(
                lesson => lesson.Id == lessonId && lesson.TutoringAgreementId == agreementId, cancellationToken))
            throw new MaterialContextNotFoundException("The lesson was not found in the specified tutoring agreement.");

        LearningMaterial.ValidateTitle(request.Title);
        var file = await validator.ValidateAsync(
            request.Content, request.FileName, request.SizeBytes, cancellationToken);
        var key = await storage.SaveAsync(request.Content, file.Extension, cancellationToken);
        LearningMaterial material;
        try
        {
            material = new LearningMaterial(agreementId, lessonId, request.UserAccountId, request.Title,
                new FileDescriptor(file.FileName, file.ContentType, new FileSize(file.SizeBytes),
                    new StorageLocation(key)), timeProvider.GetUtcNow());
            dbContext.LearningMaterials.Add(material);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            // Request cancellation must not prevent compensation.
            try { await storage.DeleteAsync(key, CancellationToken.None); }
            catch (Exception cleanupError)
            {
                logger.LogError(cleanupError, "Failed to clean up upload {StorageKey}. RequestMetadata: {RequestMetadata}",
                    key, request.RequestMetadata);
            }
            if (exception is OperationCanceledException)
                throw;
            throw new MaterialUploadFailedException(exception);
        }
        logger.LogInformation(
            "Learning material {MaterialId} uploaded by {UserAccountId} to agreement {TutoringAgreementId}, lesson {LessonId}. RequestMetadata: {RequestMetadata}",
            material.Id.Value, request.UserAccountId.Value, agreementId.Value, lessonId?.Value, request.RequestMetadata);
        return new UploadLearningMaterialResponse(material.Id.Value, agreementId.Value, lessonId?.Value,
            request.UserAccountId.Value, material.Title, file.FileName, file.ContentType, file.SizeBytes,
            material.Status.ToString(), material.CreatedAtUtc);
    }
}
