using Tutoring.Domain.Common;
using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.UnitTest.Identity;
using Tutoring.Domain.Identity;
public class EmailTest
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user@domain.com")]
    [InlineData("stecu03@gmail.com")]
    public void Create_withValidEmail_ShouldSucceed(string email)
    {
        var emailAddress = new EmailAddress(email);
        Assert.Equal(email, emailAddress.Value);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("user@.com")]
    [InlineData("user@domain")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_withInvalidEmail_ShouldThrow(string email)
    {
        Assert.Throws<InvalidEmailAddressException>(() => new EmailAddress(email));
    }


}