using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Common.Authorization;
using Tutoring.Api.Features.LearningMaterials.Exceptions;
using Tutoring.Domain.LearningMaterials;
using Tutoring.Domain.Lessons;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.LearningMaterials.AttachLearningMaterialToLesson;

public sealed class AttachLearningMaterialToLessonHandler(
    TutoringDbContext dbContext, ILogger<AttachLearningMaterialToLessonHandler> logger)
    : IRequestHandler<AttachLearningMaterialToLessonCommand>
{
    public async Task Handle(AttachLearningMaterialToLessonCommand request, CancellationToken cancellationToken)
    {
        var materialId = new LearningMaterialId(request.MaterialId);
        var material = await dbContext.LearningMaterials.SingleOrDefaultAsync(m => m.Id == materialId &&
            dbContext.TutoringAgreements.ForParticipant(request.UserAccountId)
                .Any(a => a.Id == m.TutoringAgreementId), cancellationToken);
        if (material is null)
            throw new MaterialContextNotFoundException("The learning material was not found or is not accessible to you.");

        var lessonId = new LessonId(request.LessonId);
        if (lessonId.Value == Guid.Empty)
            throw new InvalidLearningMaterialException("Lesson ID must not be empty.");
        if (!await dbContext.Lessons.AsNoTracking().AnyAsync(
                lesson => lesson.Id == lessonId && lesson.TutoringAgreementId == material.TutoringAgreementId,
                cancellationToken))
            throw new MaterialContextNotFoundException("The lesson was not found in the material's tutoring agreement.");

        material.AttachToLesson(lessonId);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Learning material {MaterialId} attached to lesson {LessonId} by {UserAccountId} in agreement {TutoringAgreementId}. RequestMetadata: {RequestMetadata}",
            material.Id.Value, lessonId.Value, request.UserAccountId.Value, material.TutoringAgreementId.Value, request.RequestMetadata);
    }
}
