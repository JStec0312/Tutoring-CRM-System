namespace Tutoring.Infrastructure.Authentication;

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    public int ResetUrlActiveHours { get; init; }

    public string ResetUrl { get; init; } = null!;
}