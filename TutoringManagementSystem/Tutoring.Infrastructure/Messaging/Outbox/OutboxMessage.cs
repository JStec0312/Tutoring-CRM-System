namespace Tutoring.Infrastructure.Mailing;


public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public string Type { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateTimeOffset? ProcessedAtUtc { get; set; }

    public int RetryCount { get; set; }

    public string? Error { get; set; }
}