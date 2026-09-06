using System.Text.Encodings.Web;

namespace Tutoring.Infrastructure.Mailing.Templates;

public static class UserRegisteredEmailTemplate
{
    public const string Subject = "Welcome to Tutoring CRM";

    public static string Render(string? firstName)
    {
        var greeting = string.IsNullOrWhiteSpace(firstName)
            ? "Cześć!"
            : $"Cześć {HtmlEncoder.Default.Encode(firstName)}!";

        return $"""
            <!DOCTYPE html>
            <html lang="pl">
            <head>
                <meta charset="UTF-8">
            </head>
            <body>
                <h1>{greeting}</h1>

                <p>Your account has been successfully created.</p>

                <p>Welcome to Tutoring CRM.</p>
            </body>
            </html>
            """;
    }
}