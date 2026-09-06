namespace Tutoring.Infrastructure.Mailing;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 1025;

    public string FromEmail { get; init; } = "noreply@tutoring.local";
    public string FromName { get; init; } = "Tutoring CRM";
}