namespace Tutoring.Infrastructure.Email;

public sealed record EmailMessage(
    string Recipient,
    string Subject,
    string HtmlBody);