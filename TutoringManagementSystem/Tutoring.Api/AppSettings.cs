namespace Tutoring.Api;

public static class AppSettings
{
    public static PasswordPolicyOptions PasswordPolicy { get; } = new();

    public sealed class PasswordPolicyOptions
    {
        public int MinimumLength { get; init; } = 8;
        public int MaximumLength { get; init; } = 128;
        public bool RequireUppercase { get; init; } = true;
        public bool RequireLowercase { get; init; } = true;
        public bool RequireDigit { get; init; } = true;
        public bool RequireSpecialCharacter { get; init; } = true;
    }
}