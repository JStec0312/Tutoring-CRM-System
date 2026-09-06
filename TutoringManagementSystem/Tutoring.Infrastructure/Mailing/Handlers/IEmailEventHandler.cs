namespace Tutoring.Infrastructure.Mailing.Handlers;

public interface IEmailEventHandler
{
    string EventType { get; }

    Task HandleAsync(
        string payload,
        CancellationToken cancellationToken);
}