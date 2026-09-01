namespace Tutoring.Infrastructure.Messaging.Contracts;

public interface IPublisher
{
        Task PublishAsync(
                Guid messageId,
                string type,
                string payload,
                CancellationToken cancellationToken);
}