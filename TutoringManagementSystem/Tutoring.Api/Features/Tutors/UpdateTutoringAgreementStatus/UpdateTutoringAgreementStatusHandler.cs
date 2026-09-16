using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.UpdateTutoringAgreementStatus;

public sealed class UpdateTutoringAgreementStatusHandler(
    TutoringDbContext dbContext,
    ILogger<UpdateTutoringAgreementStatusHandler> logger)
    : IRequestHandler<UpdateTutoringAgreementStatusCommand>
{
    public async Task Handle(
        UpdateTutoringAgreementStatusCommand request,
        CancellationToken cancellationToken)
    {
        var tutor = await dbContext.Tutors
            .SingleOrDefaultAsync(
                tutor => tutor.UserAccountId == request.UserAccountId,
                cancellationToken);

        if (tutor is null)
        {
            logger.LogWarning(
                "Tutor not found while updating tutoring agreement status. UserId: {UserId}, TutoringAgreementId: {TutoringAgreementId}, RequestMetadata: {RequestMetadata}",
                request.UserAccountId.Value,
                request.TutoringAgreementId,
                request.RequestMetadata);

            throw new TutorNotFoundException(request.UserAccountId.Value);
        }

        // Ownership is enforced by requiring the agreement to belong to the
        // authenticated tutor; a mismatch is treated the same as "not found".
        var agreement = await dbContext.TutoringAgreements
            .SingleOrDefaultAsync(
                agreement =>
                    agreement.Id == new TutoringAgreementId(request.TutoringAgreementId) &&
                    agreement.TutorId == tutor.Id &&
                    agreement.Status != AgreementStatus.Ended,
                cancellationToken);

        if (agreement is null)
        {
            logger.LogWarning(
                "Tutoring agreement not found or not owned by tutor while updating tutoring agreement status. TutorId: {TutorId}, TutoringAgreementId: {TutoringAgreementId}, RequestMetadata: {RequestMetadata}",
                tutor.Id.Value,
                request.TutoringAgreementId,
                request.RequestMetadata);

            throw new TutoringAgreementNotFoundByIdException(
                tutor.Id.Value,
                request.TutoringAgreementId);
        }

        if (request.IsActive)
        {
            agreement.Activate();
        }
        else
        {
            agreement.Deactivate();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Tutoring agreement status updated. TutorId: {TutorId}, TutoringAgreementId: {TutoringAgreementId}, Status: {Status}, RequestMetadata: {RequestMetadata}",
            tutor.Id.Value,
            agreement.Id.Value,
            agreement.Status,
            request.RequestMetadata);
    }
}
