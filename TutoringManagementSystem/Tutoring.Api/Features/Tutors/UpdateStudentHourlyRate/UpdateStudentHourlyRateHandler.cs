using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Common;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.UpdateStudentHourlyRate;

public sealed class UpdateStudentHourlyRateHandler(
    TutoringDbContext dbContext,
    ILogger<UpdateStudentHourlyRateHandler> logger)
    : IRequestHandler<UpdateStudentHourlyRateCommand>
{
    public async Task Handle(
        UpdateStudentHourlyRateCommand request,
        CancellationToken cancellationToken)
    {
        var tutor = await dbContext.Tutors
            .SingleOrDefaultAsync(
                tutor => tutor.UserAccountId == request.UserAccountId,
                cancellationToken);

        if (tutor is null)
        {
            logger.LogWarning(
                "Tutor not found while updating student hourly rate. UserId: {UserId}, StudentId: {StudentId}, RequestMetadata: {RequestMetadata}",
                request.UserAccountId.Value,
                request.StudentId,
                request.RequestMetadata);

            throw new TutorNotFoundException(request.UserAccountId.Value);
        }

        var agreement = await dbContext.TutoringAgreements
            .SingleOrDefaultAsync(
                agreement =>
                    agreement.TutorId == tutor.Id &&
                    agreement.StudentId == new StudentId(request.StudentId) &&
                    agreement.Status != AgreementStatus.Ended,
                cancellationToken);

        if (agreement is null)
        {
            logger.LogWarning(
                "Tutoring agreement not found while updating student hourly rate. TutorId: {TutorId}, StudentId: {StudentId}, RequestMetadata: {RequestMetadata}",
                tutor.Id.Value,
                request.StudentId,
                request.RequestMetadata);

            throw new TutoringAgreementNotFoundException(
                tutor.Id.Value,
                request.StudentId);
        }

        HourlyRate? hourlyRate = request.HourlyRate.HasValue
            ? new HourlyRate(
                new Money(
                    request.HourlyRate.Value,
                    new Currency("PLN")))
            : null;

        agreement.UpdateHourlyRate(hourlyRate);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Student hourly rate updated. TutorId: {TutorId}, StudentId: {StudentId}, AgreementId: {AgreementId}, RequestMetadata: {RequestMetadata}",
            tutor.Id.Value,
            request.StudentId,
            agreement.Id.Value,
            request.RequestMetadata);
    }
}
