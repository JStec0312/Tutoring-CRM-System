using System.Text.Json;
using Microsoft.Extensions.Options;
using Tutoring.Infrastructure.Authentication;
using Tutoring.Infrastructure.Mailing.Templates;
using Tutoring.Infrastructure.Messaging.Contracts;

namespace Tutoring.Infrastructure.Mailing.Handlers;

public sealed class UserRegisteredEmailHandler(
    IMailer mailer,
    IOptions<EmailVerificationOptions> options)
    : IEmailEventHandler
{
    private readonly EmailVerificationOptions _options =
        options.Value;

    public string EventType =>
        UserRegisteredIntegrationEvent.EventType;

    public async Task HandleAsync(
        string payload,
        CancellationToken cancellationToken)
    {
        var integrationEvent =
            JsonSerializer.Deserialize<UserRegisteredIntegrationEvent>(
                payload)
            ?? throw new JsonException(
                "Could not deserialize UserRegisteredIntegrationEvent.");

        var confirmationUrl =
            $"{_options.ConfirmationUrl}?token=" +
            Uri.EscapeDataString(
                integrationEvent.VerificationToken);

        await mailer.SendAsync(
            recipient: integrationEvent.Email,
            subject: UserRegisteredEmailTemplate.Subject,
            htmlBody: UserRegisteredEmailTemplate.Render(
                integrationEvent.FirstName,
                confirmationUrl),
            cancellationToken: cancellationToken);
    }
}