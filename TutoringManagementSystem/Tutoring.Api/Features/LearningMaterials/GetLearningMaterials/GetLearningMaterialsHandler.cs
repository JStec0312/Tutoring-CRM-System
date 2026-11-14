using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Common.Authorization;
using Tutoring.Api.Features.LearningMaterials.Exceptions;
using Tutoring.Domain.LearningMaterials;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.LearningMaterials.GetLearningMaterials;

public sealed class GetLearningMaterialsHandler(TutoringDbContext db)
    : IRequestHandler<GetLearningMaterialsQuery, IReadOnlyList<LearningMaterialResponse>>
{
    public async Task<IReadOnlyList<LearningMaterialResponse>> Handle(
        GetLearningMaterialsQuery request, CancellationToken cancellationToken)
    {
        var agreement = await db.TutoringAgreements.AsNoTracking().ForParticipant(request.AccountId)
            .Where(agreement => agreement.Id == request.AgreementId)
            .Select(agreement => new { agreement.Tutor.UserAccountId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new MaterialContextNotFoundException("The tutoring agreement was not found or is not accessible to you.");

        return await db.LearningMaterials.AsNoTracking()
            .Where(material => material.TutoringAgreementId == request.AgreementId &&
                (material.Status == MaterialStatus.Published || material.Status == MaterialStatus.Draft))
            .OrderByDescending(material => material.CreatedAtUtc)
            .Select(material => new LearningMaterialResponse(
                material.Id.Value, material.LessonId == null ? null : material.LessonId.Value.Value,
                material.Title, material.File.FileName, material.File.ContentType, material.File.Size.Bytes,
                material.UploadedByUserAccountId == agreement.UserAccountId))
            .ToListAsync(cancellationToken);
    }
}
