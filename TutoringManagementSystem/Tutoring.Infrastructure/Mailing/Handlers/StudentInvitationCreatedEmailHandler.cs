using System.Text.Json;
using Microsoft.Extensions.Options;
using Tutoring.Infrastructure.Mailing.Templates;
using Tutoring.Infrastructure.Messaging.Contracts;
using Tutoring.Infrastructure.StudentInvitations;

namespace Tutoring.Infrastructure.Mailing.Handlers;

public sealed class StudentInvitationCreatedEmailHandler(
    IMailer mailer,
    IOptions<StudentInvitationOptions> options)
    : IEmailEventHandler
{
    private readonly StudentInvitationOptions _options =
        options.Value;

    public string EventType =>
        StudentInvitationCreatedIntegrationEvent.EventType;

    public async Task HandleAsync(
        string payload,
        CancellationToken cancellationToken)
    {
        var integrationEvent =
            JsonSerializer.Deserialize<StudentInvitationCreatedIntegrationEvent>(
                payload)
            ?? throw new JsonException(
                "Could not deserialize StudentInvitationCreatedIntegrationEvent.");

        var invitationUrl =
            $"{_options.FrontendBaseUrl.TrimEnd('/')}" +
            $"/invitations/{Uri.EscapeDataString(integrationEvent.InvitationToken)}";

        await mailer.SendAsync(
            recipient: integrationEvent.Email,
            subject: StudentInvitationEmailTemplate.Subject,
            htmlBody: StudentInvitationEmailTemplate.Render(
                integrationEvent.Title,
                integrationEvent.Subject,
                invitationUrl,
                integrationEvent.ValidUntilUtc),
            cancellationToken: cancellationToken);
    }
}