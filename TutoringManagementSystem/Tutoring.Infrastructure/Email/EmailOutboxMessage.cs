namespace Tutoring.Infrastructure.Email;

public sealed class EmailOutboxMessage
{
    private EmailOutboxMessage()
    {
    }
    public Guid Id { get; private set; }
    public string Recipient { get; private set; }
    public string Subject { get; private set; }
    public string HtmlBody { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? SentAtUtc { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
        public EmailOutboxMessage(
        string recipient,
        string subject,
        string htmlBody,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        Recipient = recipient;
        Subject = subject;
        HtmlBody = htmlBody;
        CreatedAtUtc = createdAtUtc;
    }

    public void MarkAsSent(DateTime sentAtUtc)
    {
        SentAtUtc = sentAtUtc;
    }

    public void MarkAsFailed(string error)
    {
        Attempts++;
        LastError = error;
    }
}