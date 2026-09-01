namespace Tutoring.Infrastructure.Messaging.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int ProcessingIntervalSeconds { get; init; } = 2;

    public int BatchSize { get; init; } = 20;
}