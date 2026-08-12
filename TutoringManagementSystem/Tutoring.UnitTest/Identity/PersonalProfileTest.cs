using Tutoring.Domain.Common;
using Tutoring.Domain.Identity;

namespace Tutoring.UnitTest.Identity;

public class PersonalProfileTest
{
    [Fact]
    public void Create_withValidData_ShouldSucceed()
    {
        var phoneNumber = new PhoneNumber("123456789");

        var profile = new PersonalProfile("john.doe", "John", "Doe", phoneNumber);

        Assert.Equal("john.doe", profile.UserName);
        Assert.Equal("John", profile.FirstName);
        Assert.Equal("Doe", profile.LastName);
        Assert.Equal(phoneNumber, profile.PhoneNumber);
    }

    [Fact]
    public void Create_withoutOptionalFields_ShouldSucceed()
    {
        var profile = new PersonalProfile("john.doe", null, null, null);

        Assert.Equal("john.doe", profile.UserName);
        Assert.Null(profile.FirstName);
        Assert.Null(profile.LastName);
        Assert.Null(profile.PhoneNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_withInvalidUserName_ShouldThrow(string userName)
    {
        Assert.Throws<ArgumentException>(() => new PersonalProfile(userName, null, null, null));
    }
}
