namespace Tutoring.Infrastructure.StudentInvitations;

public sealed class StudentInvitationOptions
{
    public const string SectionName = "StudentInvitations";

    public int ValidDays { get; init; } = 7;

    public string FrontendBaseUrl { get; init; } = null!;
}

