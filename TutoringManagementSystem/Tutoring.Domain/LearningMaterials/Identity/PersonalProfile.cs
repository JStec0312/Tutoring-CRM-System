using Tutoring.Domain.Common;

namespace Tutoring.Domain.Identity;

public sealed record PersonalProfile
{
    private PersonalProfile()
    {
    }

    public PersonalProfile(
        string userName,
        string? firstName,
        string? lastName,
        PhoneNumber? phoneNumber)
    {
        UserName = Guard.NotBlank(userName, nameof(userName));
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
    }

    public string UserName { get; private set; } = null!;
    public string? FirstName { get; private set; } = null!;

    public string? LastName { get; private set; } = null!;

    public PhoneNumber? PhoneNumber { get; private set; }
}
