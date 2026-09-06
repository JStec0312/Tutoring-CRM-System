using Tutoring.Infrastructure.Mailing.Handlers;

namespace Tutoring.Infrastructure.Mailing;

public sealed class EmailEventDispatcher
{
    private readonly IReadOnlyDictionary<string, IEmailEventHandler> _handlers;

    public IReadOnlyCollection<string> SupportedEventTypes { get; }

    public EmailEventDispatcher(
        IEnumerable<IEmailEventHandler> handlers)
    {
        var handlerArray = handlers.ToArray();

        _handlers = handlerArray.ToDictionary(
            handler => handler.EventType,
            StringComparer.Ordinal);

        SupportedEventTypes = handlerArray
            .Select(handler => handler.EventType)
            .ToArray();
    }

    public Task DispatchAsync(
        string eventType,
        string payload,
        CancellationToken cancellationToken)
    {
        if (!_handlers.TryGetValue(eventType, out var handler))
        {
            throw new InvalidOperationException(
                $"No email handler registered for event type '{eventType}'.");
        }

        return handler.HandleAsync(
            payload,
            cancellationToken);
    }
}