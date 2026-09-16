using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.AddStudentManually;

public sealed class AddStudentManuallyHandler(
    TutoringDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<AddStudentManuallyHandler> logger)
    : IRequestHandler<
        AddStudentManuallyCommand,
        AddStudentManuallyResponse>
{
    public async Task<AddStudentManuallyResponse> Handle(
        AddStudentManuallyCommand request,
        CancellationToken cancellationToken)
    {
        var tutor = await dbContext.Tutors
            .SingleOrDefaultAsync(
                tutor => tutor.UserAccountId == request.UserAccountId,
                cancellationToken);

        if (tutor is null)
        {
            logger.LogWarning(
                "Tutor not found while manually adding student. UserId: {UserId}, RequestMetadata: {RequestMetadata}",
                request.UserAccountId.Value,
                request.RequestMetadata);

            throw new TutorNotFoundException(
                request.UserAccountId.Value);
        }

        var now = timeProvider.GetUtcNow();

        var displayName = new StudentDisplayName(
            request.DisplayName);

        var agreementTitle = new AgreementTitle(
            request.Title);

        var subject = new Subject(
            request.Subject);

        HourlyRate? hourlyRate = null;

        if (request.HourlyRate.HasValue)
        {
            hourlyRate = new HourlyRate(
                new Money(
                    request.HourlyRate.Value,
                    new Currency("PLN")));
        }

        var student = new Student(
            displayName,
            now);

        var agreement = new TutoringAgreement(
            tutor.Id,
            student.Id,
            subject,
            hourlyRate,
            agreementTitle,
            now);

        dbContext.Students.Add(student);
        dbContext.TutoringAgreements.Add(agreement);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        logger.LogInformation(
            "Student manually added. StudentId: {StudentId}, AgreementId: {AgreementId}, TutorId: {TutorId}, RequestMetadata: {RequestMetadata}",
            student.Id.Value,
            agreement.Id.Value,
            tutor.Id.Value,
            request.RequestMetadata);

        return new AddStudentManuallyResponse(
            student.Id.Value,
            agreement.Id.Value);
    }
}