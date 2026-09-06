using System.Text.Json;
using Tutoring.Infrastructure.Mailing.Templates;
using Tutoring.Infrastructure.Messaging.Contracts;

namespace Tutoring.Infrastructure.Mailing.Handlers;

public sealed class UserRegisteredEmailHandler(
    IMailer mailer)
    : IEmailEventHandler
{
    public string EventType =>
        UserRegisteredIntegrationEvent.EventType;

    public async Task HandleAsync(
        string payload,
        CancellationToken cancellationToken)
    {
        var integrationEvent =
            JsonSerializer.Deserialize<UserRegisteredIntegrationEvent>(payload)
            ?? throw new JsonException(
                "Could not deserialize UserRegisteredIntegrationEvent.");

        await mailer.SendAsync(
            recipient: integrationEvent.Email,
            subject: UserRegisteredEmailTemplate.Subject,
            htmlBody: UserRegisteredEmailTemplate.Render(
                integrationEvent.FirstName),
            cancellationToken: cancellationToken);
    }
}