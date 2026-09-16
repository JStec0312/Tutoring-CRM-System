using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Common;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.UpdateStudentDetails;

public sealed class UpdateStudentDetailsHandler(
    TutoringDbContext dbContext,
    ILogger<UpdateStudentDetailsHandler> logger)
    : IRequestHandler<UpdateStudentDetailsCommand>
{
    public async Task Handle(
        UpdateStudentDetailsCommand request,
        CancellationToken cancellationToken)
    {
        var tutor = await dbContext.Tutors
            .SingleOrDefaultAsync(
                tutor => tutor.UserAccountId == request.UserAccountId,
                cancellationToken);

        if (tutor is null)
        {
            logger.LogWarning(
                "Tutor not found while updating student details. UserId: {UserId}, StudentId: {StudentId}, RequestMetadata: {RequestMetadata}",
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
                "Tutoring agreement not found while updating student details. TutorId: {TutorId}, StudentId: {StudentId}, RequestMetadata: {RequestMetadata}",
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

        var contactEmail = ToOptionalEmail(request.ContactEmail);
        var contactPhoneNumber = ToOptionalPhoneNumber(request.ContactPhoneNumber);
        var notes = string.IsNullOrWhiteSpace(request.Notes)
            ? null
            : request.Notes.Trim();

        agreement.UpdateStudentDetails(
            new Subject(request.Subject),
            hourlyRate,
            contactEmail,
            contactPhoneNumber,
            notes);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Student details updated. TutorId: {TutorId}, StudentId: {StudentId}, AgreementId: {AgreementId}, RequestMetadata: {RequestMetadata}",
            tutor.Id.Value,
            request.StudentId,
            agreement.Id.Value,
            request.RequestMetadata);
    }

    private static EmailAddress? ToOptionalEmail(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : new EmailAddress(value);
    }

    private static PhoneNumber? ToOptionalPhoneNumber(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : new PhoneNumber(value);
    }
}
