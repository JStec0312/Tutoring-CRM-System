using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Tutoring.Infrastructure.Mailing;

namespace Tutoring.Infrastructure.Messaging.RabbitMq;

public sealed class EmailConsumer(
    IOptions<RabbitMqOptions> options,
    EmailEventDispatcher dispatcher,
    ILogger<EmailConsumer> logger)
    : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var rabbitOptions = options.Value;

        var factory = new ConnectionFactory
        {
            HostName = rabbitOptions.HostName,
            Port = rabbitOptions.Port,
            UserName = rabbitOptions.UserName,
            Password = rabbitOptions.Password,
            VirtualHost = rabbitOptions.VirtualHost
        };

        _connection =
            await factory.CreateConnectionAsync(stoppingToken);

        _channel = await _connection.CreateChannelAsync(
            cancellationToken: stoppingToken);

        await DeclareTopologyAsync(
            _channel,
            rabbitOptions,
            dispatcher.SupportedEventTypes,
            stoppingToken);

        var consumer =
            new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, args) =>
        {
            var payload =
                Encoding.UTF8.GetString(args.Body.Span);

            try
            {
                await dispatcher.DispatchAsync(
                    eventType: args.RoutingKey,
                    payload: payload,
                    cancellationToken: stoppingToken);

                logger.LogInformation(
                    "Email event processed. EventType: {EventType}, MessageId: {MessageId}",
                    args.RoutingKey,
                    args.BasicProperties.MessageId);

                await _channel.BasicAckAsync(
                    deliveryTag: args.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);
            }
            catch (JsonException exception)
            {
                logger.LogError(
                    exception,
                    "Invalid email event payload. EventType: {EventType}, MessageId: {MessageId}",
                    args.RoutingKey,
                    args.BasicProperties.MessageId);

                await _channel.BasicNackAsync(
                    deliveryTag: args.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to process email event. EventType: {EventType}, MessageId: {MessageId}",
                    args.RoutingKey,
                    args.BasicProperties.MessageId);

                await _channel.BasicNackAsync(
                    deliveryTag: args.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: rabbitOptions.EmailQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);
    }

    private static async Task DeclareTopologyAsync(
        IChannel channel,
        RabbitMqOptions options,
        IEnumerable<string> eventTypes,
        CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: options.EmailQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        foreach (var eventType in eventTypes)
        {
            await channel.QueueBindAsync(
                queue: options.EmailQueueName,
                exchange: options.ExchangeName,
                routingKey: eventType,
                cancellationToken: cancellationToken);
        }
    }

    public override async Task StopAsync(
        CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}