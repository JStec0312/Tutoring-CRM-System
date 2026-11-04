using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Common.Authorization;
using Tutoring.Api.Features.LearningMaterials.Exceptions;
using Tutoring.Domain.LearningMaterials;
using Tutoring.Infrastructure.FileStorage;
using Tutoring.Infrastructure.Mailing;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.LearningMaterials.DeleteLearningMaterial;

public sealed class DeleteLearningMaterialHandler(
    TutoringDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<DeleteLearningMaterialHandler> logger) : IRequestHandler<DeleteLearningMaterialCommand>
{
    public async Task Handle(DeleteLearningMaterialCommand request, CancellationToken cancellationToken)
    {
        var materialId = new LearningMaterialId(request.MaterialId);
        var tutorAgreementIds = dbContext.TutoringAgreements.ForTutor(request.UserAccountId)
            .Select(agreement => agreement.Id);
        var studentAgreementIds = dbContext.TutoringAgreements
            .Where(agreement => agreement.Student.UserAccountId == request.UserAccountId)
            .Select(agreement => agreement.Id);
        var ownedMaterial = dbContext.LearningMaterials.Where(material => material.Id == materialId &&
            ((request.IsTutor && tutorAgreementIds.Contains(material.TutoringAgreementId)) ||
             (material.UploadedByUserAccountId == request.UserAccountId &&
              studentAgreementIds.Contains(material.TutoringAgreementId))));

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var changed = await ownedMaterial.Where(material => material.Status != MaterialStatus.Deleted)
            .ExecuteUpdateAsync(update => update.SetProperty(material => material.Status, MaterialStatus.Deleted),
                cancellationToken);
        if (changed == 0)
        {
            if (!await ownedMaterial.AnyAsync(cancellationToken))
                throw new MaterialContextNotFoundException("The learning material was not found or is not accessible to you.");

            return;
        }

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = LearningMaterialCleanup.MessageType,
            Payload = materialId.Value.ToString("D"),
            OccurredAtUtc = timeProvider.GetUtcNow()
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Learning material {MaterialId} marked for deletion by {UserAccountId}. RequestMetadata: {RequestMetadata}",
            materialId.Value, request.UserAccountId.Value, request.RequestMetadata);
    }
}
