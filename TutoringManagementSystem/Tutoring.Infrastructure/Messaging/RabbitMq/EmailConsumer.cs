using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Tutoring.Infrastructure.Messaging.Contracts;

namespace Tutoring.Infrastructure.Messaging.RabbitMq;

public sealed class EmailConsumer(
    IOptions<RabbitMqOptions> options,
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

        _connection = await factory.CreateConnectionAsync(stoppingToken);

        _channel = await _connection.CreateChannelAsync(
            cancellationToken: stoppingToken);

        await DeclareTopologyAsync(
            _channel,
            rabbitOptions,
            stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, args) =>
        {
            var message = Encoding.UTF8.GetString(args.Body.Span);

            logger.LogInformation(
                "Received RabbitMQ message: {Message}",
                message);

            await _channel.BasicAckAsync(
                deliveryTag: args.DeliveryTag,
                multiple: false,
                cancellationToken: stoppingToken);
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

        await channel.QueueBindAsync(
            queue: options.EmailQueueName,
            exchange: options.ExchangeName,
            routingKey: UserRegisteredIntegrationEvent.EventType,
            cancellationToken: cancellationToken);
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