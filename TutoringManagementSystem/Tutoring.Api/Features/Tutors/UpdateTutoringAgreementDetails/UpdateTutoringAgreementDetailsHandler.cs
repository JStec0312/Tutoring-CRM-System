using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.UpdateTutoringAgreementDetails;

public sealed class UpdateTutoringAgreementDetailsHandler(
    TutoringDbContext dbContext,
    ILogger<UpdateTutoringAgreementDetailsHandler> logger)
    : IRequestHandler<UpdateTutoringAgreementDetailsCommand>
{
    public async Task Handle(
        UpdateTutoringAgreementDetailsCommand request,
        CancellationToken cancellationToken)
    {
        var tutor = await dbContext.Tutors
            .SingleOrDefaultAsync(
                tutor => tutor.UserAccountId == request.UserAccountId,
                cancellationToken);

        if (tutor is null)
        {
            logger.LogWarning(
                "Tutor not found while updating tutoring agreement details. UserId: {UserId}, TutoringAgreementId: {TutoringAgreementId}, RequestMetadata: {RequestMetadata}",
                request.UserAccountId.Value,
                request.TutoringAgreementId,
                request.RequestMetadata);

            throw new TutorNotFoundException(request.UserAccountId.Value);
        }

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
                "Tutoring agreement not found or not owned by tutor while updating details. TutorId: {TutorId}, TutoringAgreementId: {TutoringAgreementId}, RequestMetadata: {RequestMetadata}",
                tutor.Id.Value,
                request.TutoringAgreementId,
                request.RequestMetadata);

            throw new TutoringAgreementNotFoundByIdException(
                tutor.Id.Value,
                request.TutoringAgreementId);
        }


        var subject = request.Subject is null ? null : new Subject(request.Subject);
        agreement.UpdateTutoringAgreementDetails(
            subject,
            request.Notes);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Tutoring agreement details updated. TutorId: {TutorId}, TutoringAgreementId: {TutoringAgreementId}, RequestMetadata: {RequestMetadata}",
            tutor.Id.Value,
            agreement.Id.Value,
            request.RequestMetadata);
    }
}
