using System.Text.Encodings.Web;

namespace Tutoring.Infrastructure.Mailing.Templates;

public static class UserRegisteredEmailTemplate
{
    public const string Subject =
        "Confirm your Tutoring CRM account";

    public static string Render(
        string? firstName,
        string confirmationUrl)
    {
        var greeting = string.IsNullOrWhiteSpace(firstName)
            ? "Cześć!"
            : $"Cześć {HtmlEncoder.Default.Encode(firstName)}!";

        var encodedConfirmationUrl =
            HtmlEncoder.Default.Encode(confirmationUrl);

        return $"""
            <!DOCTYPE html>
            <html lang="pl">
            <head>
                <meta charset="UTF-8">
            </head>
            <body>
                <h1>{greeting}</h1>

                <p>
                    Twoje konto zostało utworzone.
                </p>

                <p>
                    Kliknij poniższy link, aby potwierdzić adres e-mail:
                </p>

                <p>
                    <a href="{encodedConfirmationUrl}">
                        Potwierdź adres e-mail
                    </a>
                </p>

                <p>
                    Link jest ważny przez 24 godziny.
                </p>

                <p>
                    Jeśli to nie Ty zakładałeś konto,
                    zignoruj tę wiadomość.
                </p>
            </body>
            </html>
            """;
    }
}