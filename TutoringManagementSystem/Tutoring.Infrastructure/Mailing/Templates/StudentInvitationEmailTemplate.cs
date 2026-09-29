using System.Text.Encodings.Web;

namespace Tutoring.Infrastructure.Mailing.Templates;

public static class StudentInvitationEmailTemplate
{
    public const string Subject =
        "You have been invited to Tutoring CRM";

    public static string Render(
        string title,
        string subject,
        string invitationUrl,
        DateTimeOffset validUntilUtc)
    {
        var encodedTitle =
            HtmlEncoder.Default.Encode(title);

        var encodedSubject =
            HtmlEncoder.Default.Encode(subject);

        var encodedUrl =
            HtmlEncoder.Default.Encode(invitationUrl);

        return $"""
            <!DOCTYPE html>
            <html lang="pl">
            <head>
                <meta charset="UTF-8">
            </head>
            <body>
                <h1>Otrzymałeś zaproszenie</h1>

                <p>
                    Korepetytor zaprosił Cię do współpracy
                    w Tutoring CRM.
                </p>

                <p>
                    <strong>{encodedTitle}</strong><br />
                    Przedmiot: {encodedSubject}
                </p>

                <p>
                    <a href="{encodedUrl}">
                        Otwórz zaproszenie
                    </a>
                </p>

                <p>
                    Zaproszenie jest ważne do:
                    {validUntilUtc:yyyy-MM-dd HH:mm} UTC.
                </p>
            </body>
            </html>
            """;
    }
}