namespace Tutoring.Infrastructure.Authentication;

public sealed class EmailVerificationOptions
{
    public const string SectionName = "EmailVerification";

    public int ConfirmationUrlActiveHours { get; init; }
    public string ConfirmationUrl { get; init; } = null!;
}