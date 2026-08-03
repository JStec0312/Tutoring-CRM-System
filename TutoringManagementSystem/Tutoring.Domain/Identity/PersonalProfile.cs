using Tutoring.Domain.Common;

namespace Tutoring.Domain.Identity;

public sealed record PersonalProfile
{
    private PersonalProfile()
    {
    }

    public PersonalProfile(
        string firstName,
        string lastName,
        PhoneNumber? phoneNumber)
    {
        FirstName = Guard.NotBlank(firstName, nameof(firstName));
        LastName = Guard.NotBlank(lastName, nameof(lastName));
        PhoneNumber = phoneNumber;
    }

    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    public PhoneNumber? PhoneNumber { get; private set; }
}
