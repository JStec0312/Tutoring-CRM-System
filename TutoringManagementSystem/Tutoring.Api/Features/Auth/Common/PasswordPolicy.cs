namespace Tutoring.Api.Features.Common;

public sealed class PasswordPolicy
{
    public const string SectionName = "PasswordPolicy";

    public int MinimumLength { get; init; }
    public int MaximumLength { get; init; }
    public bool RequireUppercase { get; init; }
    public bool RequireLowercase { get; init; }
    public bool RequireDigit { get; init; }
    public bool RequireSpecialCharacter { get; init; }
}
