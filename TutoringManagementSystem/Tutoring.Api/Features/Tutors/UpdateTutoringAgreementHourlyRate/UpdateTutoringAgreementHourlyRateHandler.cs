using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.Billing;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.UpdateTutoringAgreementHourlyRate;

public sealed class UpdateTutoringAgreementHourlyRateHandler(
    TutoringDbContext dbContext,
    ILogger<UpdateTutoringAgreementHourlyRateHandler> logger)
    : IRequestHandler<UpdateTutoringAgreementHourlyRateCommand>
{
    public async Task Handle(
        UpdateTutoringAgreementHourlyRateCommand request,
        CancellationToken cancellationToken)
    {
        var tutor = await dbContext.Tutors
            .SingleOrDefaultAsync(
                tutor => tutor.UserAccountId == request.UserAccountId,
                cancellationToken);

        if (tutor is null)
        {
            logger.LogWarning(
                "Tutor not found while updating tutoring agreement hourly rate. UserId: {UserId}, TutoringAgreementId: {TutoringAgreementId}, RequestMetadata: {RequestMetadata}",
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
                "Tutoring agreement not found or not owned by tutor while updating hourly rate. TutorId: {TutorId}, TutoringAgreementId: {TutoringAgreementId}, RequestMetadata: {RequestMetadata}",
                tutor.Id.Value,
                request.TutoringAgreementId,
                request.RequestMetadata);

            throw new TutoringAgreementNotFoundByIdException(
                tutor.Id.Value,
                request.TutoringAgreementId);
        }

        var hourlyRate =  request.HourlyRate is null ? null : new HourlyRate(
                new Money(
                    request.HourlyRate.Value,
                    new Currency("PLN")));

        agreement.UpdateHourlyRate(hourlyRate);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Tutoring agreement hourly rate updated. TutorId: {TutorId}, TutoringAgreementId: {TutoringAgreementId}, RequestMetadata: {RequestMetadata}",
            tutor.Id.Value,
            agreement.Id.Value,
            request.RequestMetadata);
    }
}
