using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Students.AcceptStudentInvitation;
using Tutoring.Api.Features.Students.Exceptions;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Common;
using Tutoring.Domain.StudentInvitations;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.StudentInvitations.AcceptStudentInvitation;

public sealed class AcceptStudentInvitationHandler(
    TutoringDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<AcceptStudentInvitationHandler> logger)
    : IRequestHandler<
        AcceptStudentInvitationCommand,
        AcceptStudentInvitationResponse>
{
    public async Task<AcceptStudentInvitationResponse> Handle(
        AcceptStudentInvitationCommand request,
        CancellationToken cancellationToken)
    {
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    request.Token)));

        var invitation = await dbContext.StudentInvitations
            .SingleOrDefaultAsync(
                invitation =>
                    invitation.TokenHash == tokenHash,
                cancellationToken);

        if (invitation is null)
        {
            logger.LogWarning(
                "Student invitation not found. UserId: {UserId}, RequestMetadata: {RequestMetadata}",
                request.UserAccountId.Value,
                request.RequestMetadata);

            throw new StudentInvitationNotFoundException();
        }

        var now = timeProvider.GetUtcNow();

        if (invitation.ValidUntilUtc <= now ||
            invitation.Status is not (
                InvitationStatus.Created or
                InvitationStatus.Sent))
        {
            logger.LogWarning(
                "Student invitation cannot be accepted. InvitationId: {InvitationId}, Status: {Status}, ValidUntilUtc: {ValidUntilUtc}, UserId: {UserId}, RequestMetadata: {RequestMetadata}",
                invitation.Id.Value,
                invitation.Status,
                invitation.ValidUntilUtc,
                request.UserAccountId.Value,
                request.RequestMetadata);

            throw new StudentInvitationUnavailableException();
        }

        var student = await dbContext.Students
            .Include(student => student.Account)
            .SingleOrDefaultAsync(
                student =>
                    student.UserAccountId ==
                    request.UserAccountId,
                cancellationToken);

        if (student is null)
        {
            logger.LogWarning(
                "Student not found while accepting invitation. InvitationId: {InvitationId}, UserId: {UserId}, RequestMetadata: {RequestMetadata}",
                invitation.Id.Value,
                request.UserAccountId.Value,
                request.RequestMetadata);

            throw new StudentNotFoundException(
                request.UserAccountId.Value);
        }

        if (!string.Equals(
                student.Account.Email.Value,
                invitation.Recipient.Value,
                StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Student invitation recipient mismatch. InvitationId: {InvitationId}, UserId: {UserId}, RequestMetadata: {RequestMetadata}",
                invitation.Id.Value,
                request.UserAccountId.Value,
                request.RequestMetadata);

            throw new StudentInvitationRecipientMismatchException();
        }

        var agreementAlreadyExists =
            await dbContext.TutoringAgreements
                .AnyAsync(
                    agreement =>
                        agreement.TutorId ==
                            invitation.TutorId &&
                        agreement.StudentId ==
                            student.Id &&
                        agreement.Status !=
                            AgreementStatus.Ended,
                    cancellationToken);

        if (agreementAlreadyExists)
        {
            logger.LogWarning(
                "Tutoring agreement already exists. InvitationId: {InvitationId}, TutorId: {TutorId}, StudentId: {StudentId}, RequestMetadata: {RequestMetadata}",
                invitation.Id.Value,
                invitation.TutorId.Value,
                student.Id.Value,
                request.RequestMetadata);

            throw new StudentAlreadyAssignedException(
                student.Account.Email.Value);
        }

        var agreementTitle =
            new AgreementTitle(
                invitation.Title.Value);
        

        var subject =
            new Subject(
                invitation.Subject.Name);

        HourlyRate? hourlyRate = null;

        if (invitation.HourlyRate is not null)
        {
            var invitationMoney =
                invitation.HourlyRate.PricePerHour;

            hourlyRate = new HourlyRate(
                new Money(
                    invitationMoney.Amount,
                    new Currency(
                        invitationMoney.Currency.Code)));
        }
        else
        {
            hourlyRate = null;
        }

        var agreement = new TutoringAgreement(
            invitation.TutorId,
            student.Id,
            subject,
            hourlyRate,
            agreementTitle,
            now);

        dbContext.TutoringAgreements.Add(
            agreement);

        invitation.Accept();

        await dbContext.SaveChangesAsync(
            cancellationToken);

        logger.LogInformation(
            "Student invitation accepted. InvitationId: {InvitationId}, AgreementId: {AgreementId}, TutorId: {TutorId}, StudentId: {StudentId}, RequestMetadata: {RequestMetadata}",
            invitation.Id.Value,
            agreement.Id.Value,
            invitation.TutorId.Value,
            student.Id.Value,
            request.RequestMetadata);

        return new AcceptStudentInvitationResponse(
            agreement.Id.Value);
    }
}