

using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Tutoring.Infrastructure.Persistence;
using Tutoring.Infrastructure.StudentInvitations;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.Common;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Domain.Billing;
using Tutoring.Domain.StudentInvitations;
using Tutoring.Infrastructure.Messaging.Contracts;
using Tutoring.Infrastructure.Mailing;
using System.Text.Json;
using Tutoring.Api.Features.Students.Exceptions;

namespace Tutoring.Api.Features.Tutors.InviteStudent;

public sealed class InviteStudentHandler(
    TutoringDbContext dbContext,
    IStudentInvitationTokenGenerator tokenGenerator,
    IOptions<StudentInvitationOptions> options,
    TimeProvider timeProvider,
    ILogger<InviteStudentHandler> logger)
    : IRequestHandler<InviteStudentCommand, InviteStudentResponse>
{
    public async Task<InviteStudentResponse> Handle(
        InviteStudentCommand request,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var tutor = await dbContext.Tutors
            .SingleOrDefaultAsync(
                tutor =>
                    tutor.UserAccountId == request.UserAccountId,
                cancellationToken);

        if (tutor is null)
        {
            logger.LogWarning(
                "Tutor not found while creating student invitation. UserId: {UserId}, Recipient: {Recipient}, RequestMetadata: {RequestMetadata}",
                request.UserAccountId.Value,
                request.Email,
                request.RequestMetadata);

            throw new TutorNotFoundException(
                request.UserAccountId.Value);
        }

        var recipient = new EmailAddress(request.Email);

        var recipientIsRegisteredStudent =
            await dbContext.Students
                .AnyAsync(
                    student =>
                        student.Account != null &&
                        student.Account.Email.Value == recipient.Value,
                    cancellationToken);

        if (!recipientIsRegisteredStudent)
        {
            logger.LogWarning(
                "Student invitation recipient is not a registered student. TutorId: {TutorId}, Recipient: {Recipient}, RequestMetadata: {RequestMetadata}",
                tutor.Id.Value,
                recipient.Value,
                request.RequestMetadata);

            throw StudentNotFoundException.ForRecipientEmail();
        }

        var existingAgreement =
            await dbContext.TutoringAgreements
                .AnyAsync(
                    agreement =>
                        agreement.TutorId == tutor.Id &&
                        agreement.Status != AgreementStatus.Ended &&
                        agreement.Student.Account != null &&
                        agreement.Student.Account.Email.Value ==
                        recipient.Value,
                    cancellationToken);

        if (existingAgreement)
        {
            logger.LogWarning(
                "Student is already assigned to tutor. TutorId: {TutorId}, Recipient: {Recipient}, RequestMetadata: {RequestMetadata}",
                tutor.Id.Value,
                recipient.Value,
                request.RequestMetadata);

            throw new StudentAlreadyAssignedException(
                recipient.Value);
        }

        var previousInvitations = await dbContext.StudentInvitations
        .Where(invitation =>
            invitation.TutorId == tutor.Id &&
            invitation.Recipient.Value == recipient.Value &&
            (
                invitation.Status == InvitationStatus.Created ||
                invitation.Status == InvitationStatus.Sent
            ))
        .ToListAsync(cancellationToken);

    foreach (var previousInvitation in previousInvitations)
    {
        previousInvitation.Expire();
    }

        var title = new AgreementTitle(
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

        var generatedToken =
            tokenGenerator.Generate();

        var validUntilUtc =
            now.AddDays(options.Value.ValidDays);

        var invitation = new StudentInvitation(
            tutor.Id,
            recipient,
            generatedToken.tokenHash,
            title,
            hourlyRate,
            subject,
            validUntilUtc,
            now);

        var integrationEvent =
            new StudentInvitationCreatedIntegrationEvent(
                InvitationId: invitation.Id.Value,
                Email: recipient.Value,
                InvitationToken: generatedToken.rawToken,
                Title: title.Value,
                Subject: subject.Name,
                ValidUntilUtc: validUntilUtc);

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = StudentInvitationCreatedIntegrationEvent.EventType,
            Payload = JsonSerializer.Serialize(integrationEvent),
            OccurredAtUtc = now,
            RetryCount = 0
        };

        dbContext.StudentInvitations.Add(invitation);
        dbContext.OutboxMessages.Add(outboxMessage);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var invitationUrl =
            $"{options.Value.FrontendBaseUrl.TrimEnd('/')}" +
            $"/invitations/{Uri.EscapeDataString(generatedToken.rawToken)}";

        logger.LogInformation(
            "Student invitation created. InvitationId: {InvitationId}, TutorId: {TutorId}, Recipient: {Recipient}, RequestMetadata: {RequestMetadata}",
            invitation.Id.Value,
            tutor.Id.Value,
            recipient.Value,
            request.RequestMetadata);

        return new InviteStudentResponse(
            invitation.Id.Value,
            invitationUrl,
            invitation.ValidUntilUtc);
    }
}