using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.LearningMaterials;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Infrastructure.FileStorage;

public static class LearningMaterialCleanup
{
    public const string MessageType = "LearningMaterialCleanup";

    internal static async Task RemoveAsync(
        TutoringDbContext dbContext, IFileStorage storage, string payload, CancellationToken cancellationToken)
    {
        var materialId = new LearningMaterialId(Guid.Parse(payload));
        var material = await dbContext.LearningMaterials.SingleOrDefaultAsync(
            material => material.Id == materialId && material.Status == MaterialStatus.Deleted,
            cancellationToken);
        if (material is null)
            return;

        await storage.DeleteAsync(material.File.StorageLocation.Value, cancellationToken);
        dbContext.LearningMaterials.Remove(material);
    }
}
