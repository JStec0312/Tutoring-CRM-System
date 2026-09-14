

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

        var existingAgreement =
            await dbContext.TutoringAgreements
                .AnyAsync(
                    agreement =>
                        agreement.TutorId == tutor.Id &&
                        agreement.Status != AgreementStatus.Ended &&
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
            now
            );

        dbContext.StudentInvitations.Add(
            invitation);

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