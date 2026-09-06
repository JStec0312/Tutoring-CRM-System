namespace Tutoring.Infrastructure.Mailing;


public interface IMailer
{
    Task SendAsync(
        string recipient,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken);
}