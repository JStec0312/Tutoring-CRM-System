namespace Tutoring.Infrastructure.Mailing.Templates;

public static class PasswordResetEmailTemplate
{
    public const string Subject =
        "Reset your Tutoring CRM password";

    public static string Render(
        string? firstName,
        string resetUrl)
    {
        var greeting = string.IsNullOrWhiteSpace(firstName)
            ? "Hello"
            : $"Hello {firstName}";

        return $$"""
        <html>
        <body>
            <p>{{greeting}},</p>

            <p>
                We received a request to reset your password.
            </p>

            <p>
                <a href="{{resetUrl}}">
                    Reset password
                </a>
            </p>

            <p>
                If you did not request a password reset,
                you can ignore this email.
            </p>
        </body>
        </html>
        """;
    }
}