using System.Text.Json;
using Microsoft.Extensions.Options;
using Tutoring.Infrastructure.Authentication;
using Tutoring.Infrastructure.Mailing.Templates;
using Tutoring.Infrastructure.Messaging.Contracts;

namespace Tutoring.Infrastructure.Mailing.Handlers;

public sealed class PasswordResetRequestedEmailHandler(
    IMailer mailer,
    IOptions<PasswordResetOptions> options)
    : IEmailEventHandler
{
    private readonly PasswordResetOptions _options =
        options.Value;

    public string EventType =>
        PasswordResetRequestedIntegrationEvent.EventType;

    public async Task HandleAsync(
        string payload,
        CancellationToken cancellationToken)
    {
        var integrationEvent =
            JsonSerializer.Deserialize<
                PasswordResetRequestedIntegrationEvent>(
                payload)
            ?? throw new JsonException(
                "Could not deserialize PasswordResetRequestedIntegrationEvent.");

        var resetUrl =
            $"{_options.ResetUrl}?token=" +
            Uri.EscapeDataString(
                integrationEvent.ResetToken);

        await mailer.SendAsync(
            recipient: integrationEvent.Email,
            subject: PasswordResetEmailTemplate.Subject,
            htmlBody: PasswordResetEmailTemplate.Render(
                integrationEvent.FirstName,
                resetUrl),
            cancellationToken: cancellationToken);
    }
}